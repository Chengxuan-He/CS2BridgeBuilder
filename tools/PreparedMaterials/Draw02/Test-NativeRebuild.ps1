$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$project=Join-Path $repo 'artifacts/agent-material-env/Draw02/unity'
$work=Join-Path $repo 'artifacts/agent-material-env/Draw02/rebuild-native-v2'
$evidence=Join-Path $repo 'artifacts/prepared-materials/Draw02/rebuild-native-v2'
$source=[IO.File]::ReadAllText("$repo/tools/GpuBake/PreparedNativeCheck.Editor.cs")
$source=$source.Replace('new[] { "Paint", "Cable", "Road" }','new[] { "Cable", "Road" }')
$old='File.ReadAllText(Path.Combine(variantFolder, item.Item2 + ".cid")).Trim()'
if(!$source.Contains($old)){throw 'Review native checker zero-control identity verification.'}
$source=$source.Replace($old,'File.ReadAllText(Directory.GetFiles(Path.Combine(Path.GetDirectoryName(folder), "Shared"), "*.Texture.cid").Single()).Trim()')
$extra=@'
            var styleRoot=Path.GetDirectoryName(folder);
            var freshFiles=Directory.GetFiles(Path.Combine(styleRoot,"NativeSources"),"*.Surface",SearchOption.AllDirectories)
                .Concat(new[]{Path.Combine(styleRoot,"Paint","SkinnedPaint.Surface")}).ToArray();
            foreach(var file in freshFiles)
            {
                var fresh=FormatterServices.GetUninitializedObject(surfaceType);
                using(var reader=new BinaryReader(File.OpenRead(file)))
                {
                    surfaceType.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(fresh,new object[]{reader});
                    Require((int)surfaceType.GetProperty("materialTemplateHash").GetValue(fresh)==0,"new object native template");
                    bool nativePaint=file.EndsWith("SkinnedPaint.Surface",StringComparison.Ordinal);
                    Require((bool)surfaceType.GetProperty("isVTMaterial").GetValue(fresh)==nativePaint,"new paint preserves native VT");
                    var binary=io.GetType("Colossal.IO.BinaryReaderExtensions",true);
                    foreach(var method in new[]{"ReadStringFloatDictionary","ReadStringIntDictionary","ReadStringVectorDictionary","ReadStringColorDictionary"})
                        binary.GetMethod(method).Invoke(null,new object[]{reader});
                    Require(reader.ReadByte()==255,"new texture dictionary marker");int textures=reader.ReadInt32();
                    var readHash=core.GetType("Colossal.IO.BinaryReaderExtensions",true).GetMethod("ReadHash");
                    for(int i=0;i<textures;i++)
                    {
                        reader.ReadString();byte marker=reader.ReadByte();Require(marker==0||marker==255,"new native binding marker");
                        if(marker==255)readHash.Invoke(null,new object[]{reader});
                    }
                    var keywords=(IEnumerable)binary.GetMethod("ReadStringHashSet").Invoke(null,new object[]{reader});
                    Require(keywords.Cast<string>().Contains("_TANGENTSPACE_OCTO"),"new object tangent encoding");
                    Require(reader.BaseStream.Position==reader.BaseStream.Length,"new native descriptor fully consumed");
                }
            }
            Debug.Log("PASS fresh native objects: "+freshFiles.Length+"; paint retains native VT header; original textures not decoded or re-encoded.");
'@
$source=$source.Replace('            int lanes = 0;', $extra+"`n            int lanes = 0;")
[IO.File]::WriteAllText("$project/Assets/Editor/PreparedNativeCheck.cs",$source)
$log=Join-Path $evidence 'native-reader.log'
$arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','PreparedNativeCheck.Run',
'-gameManaged','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed"',
'-preparedPaint',('"'+$work+'/output/Draw02/Paint"'),'-quit','-logFile',('"'+$log+'"'))
$p=Start-Process -FilePath 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Draw02 native reader PID=$($p.Id)"
$p.WaitForExit()
[ordered]@{exitCode=$p.ExitCode;nativeCheckerSha256=(Get-FileHash "$project/Assets/Editor/PreparedNativeCheck.cs").Hash;log=$log}|ConvertTo-Json|Set-Content "$evidence/native-reader.json"
if($p.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern '^PASS native prepared readers:' -Quiet) -or !(Select-String -LiteralPath $log -Pattern '^PASS fresh native objects: 3;' -Quiet)){throw "Native validation failed: $log"}
Select-String -LiteralPath $log -Pattern '^PASS native prepared readers:'|ForEach-Object Line

