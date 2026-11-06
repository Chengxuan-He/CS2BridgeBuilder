using System.Security.Cryptography;

// Offline scheduling only. APF1 is an intermediate emitted by historical tools;
// the game accepts only the explicitly scheduled AoS representation.
internal static class AosPacking
{
    const int OldMagic = 0x31465041, Magic = 0x32465041, Words = 20;
    sealed record Node(int Id, int Op, int A, int B, int C, Node? Left = null, Node? Right = null);
    sealed record Store(int Op, Node Value, int Buffer, int Axis, int Mapping);
    internal static void Verify(string root,string source)
    {
        int checkedResources=0;
        foreach(string path in Directory.GetFiles(source,"*.bin",SearchOption.AllDirectories))
        {
            var bytes=File.ReadAllBytes(path);if(bytes.Length<68 || BitConverter.ToInt32(bytes,32)!=OldMagic)continue;
            string target=Path.Combine(root,"assets/HardcodedNumbers",Path.GetRelativePath(source,path));
            if(!Convert(bytes).AsSpan().SequenceEqual(File.ReadAllBytes(target)))throw new InvalidDataException("AoS regeneration differs: "+target);
            checkedResources++;
        }
        if(checkedResources==0)throw new InvalidDataException("No compiler input checked.");
        Console.WriteLine($"PASS deterministic AoS regeneration: {checkedResources} complete resources byte-identical.");
    }
    public static void ConvertAll(string root, bool apply)
    {
        int resources=0, converted=0, entries=0;
        foreach(string path in Directory.GetFiles(Path.Combine(root,"assets/HardcodedNumbers"),"*.bin",SearchOption.AllDirectories))
        {
            byte[] bytes=File.ReadAllBytes(path);
            if(bytes.Length<68 || BitConverter.ToInt32(bytes,8)==0)continue;
            int magic=BitConverter.ToInt32(bytes,32);
            if(magic!=OldMagic && magic!=Magic)continue;
            resources++;entries+=BitConverter.ToInt32(bytes,40);
            if(magic==Magic)continue;
            if(!apply)throw new InvalidDataException("Unpacked structural program: "+path);
            byte[] packed=Convert(bytes);
            string backup=Path.Combine(root,"artifacts/backups/before-aos89-code",Path.GetRelativePath(root,path));
            if(!File.Exists(backup)){Directory.CreateDirectory(Path.GetDirectoryName(backup)!);File.Copy(path,backup);}
            File.WriteAllBytes(path,packed);converted++;
        }
        if(resources==0)throw new InvalidDataException("No structure resources checked.");
        Console.WriteLine($"AoS programs: {resources} resources, {entries} entries, {converted} converted; no APF1 runtime input.");
    }
    internal static byte[] Convert(byte[] bytes)
    {
        if(!SHA256.HashData(bytes.AsSpan(0,bytes.Length-32)).AsSpan().SequenceEqual(bytes.AsSpan(bytes.Length-32)))throw new InvalidDataException("BBN checksum.");
        using var reader=new BinaryReader(new MemoryStream(bytes));
        if(reader.ReadInt32()!=0x314e4242 || reader.ReadInt32()!=1)throw new InvalidDataException("BBN header.");
        int[] lengths=Enumerable.Range(0,6).Select(_=>reader.ReadInt32()).ToArray();
        if(lengths[1]!=0 || lengths[2]!=0 || lengths[3]!=0 || lengths[5]!=0)throw new InvalidDataException("Unexpected structure array.");
        int[] old=Enumerable.Range(0,lengths[0]).Select(_=>reader.ReadInt32()).ToArray();
        // Keep IEEE bytes rather than promoting measured numbers through decimal text.
        int[] floatBits=Enumerable.Range(0,lengths[4]).Select(_=>reader.ReadInt32()).ToArray();
        if(old[0]!=OldMagic || old[1]!=1)throw new InvalidDataException("Expected compiler intermediate.");
        var words=new List<int>{Magic,2,old[2]};words.AddRange(new int[old[2]]);
        var values=new List<int>();
        for(int e=0;e<old[2];e++)
        {
            int start=old[3+e], count=old[start], instructions=old[start+2];
            var nodes=new Dictionary<int,Node>();var stores=new List<Store>();var ordered=new List<(Node? Node,Store? Store)>();
            var written=new HashSet<int>();bool dependent=false;
            for(int i=0;i<instructions;i++)
            {
                int p=start+3+8*i,op=old[p],dst=old[p+1],a=old[p+2],b=old[p+3],c=old[p+4];
                if(op is 10 or 11)
                {
                    var store=new Store(op,nodes[dst],a,op==10?b:0,op==10?c:b);
                    stores.Add(store);ordered.Add((null,store));written.Add(op==10?b:3);continue;
                }
                if(op==2 && written.Contains(b) || op==3 && written.Contains(3))dependent=true;
                var node=new Node(i,op,a,b,c,op>=4?nodes[a]:null,op is >=4 and <=7?nodes[b]:null);
                nodes[dst]=node;ordered.Add((node,null));
            }
            bool scalar=stores.All(s=>s.Op==11);int width=scalar?1:3;
            var code=new List<int[]>();int registers=0;
            var cache=new Dictionary<string,int>();
            int[] Instruction(int op,int dst,int mask,int a=0,int b=0)
            {var result=new int[Words];result[0]=op;result[1]=dst;result[2]=mask;result[3]=a;result[4]=b;code.Add(result);return result;}
            int Mask(Node?[] tuple){int m=0;for(int k=0;k<width;k++)if(tuple[k]!=null)m|=1<<k;return m;}
            void Descriptor(int[] instruction,int lane,Node node)
            {
                int p=5+5*lane;instruction[p]=node.Op;
                instruction[p+1]=node.A;instruction[p+2]=node.B;instruction[p+3]=node.C;
            }
            int Pack(Node?[] tuple)
            {
                string key=string.Join(",",tuple.Select(n=>n?.Id.ToString()??"-"));
                if(cache.TryGetValue(key,out int found))return found;
                int result=registers++;cache.Add(key,result);
                foreach(var group in tuple.Where(n=>n!=null).Select(n=>n!.Op<=3?0:n.Op).Distinct())
                {
                    var selected=tuple.Select(n=>n!=null && (n.Op<=3?0:n.Op)==group?n:null).ToArray();int mask=Mask(selected);
                    if(group==0)
                    {
                        var command=Instruction(0,result,mask);
                        for(int lane=0;lane<width;lane++)if(selected[lane]!=null)Descriptor(command,lane,selected[lane]!);
                    }
                    else
                    {
                        int a=Pack(selected.Select(n=>n?.Left).ToArray());
                        int b=group<=7?Pack(selected.Select(n=>n?.Right).ToArray()):0;
                        Instruction(group,result,mask,a,b);
                    }
                }
                return result;
            }
            void Output(Store?[] tuple,int register)
            {
                int mask=0;for(int lane=0;lane<width;lane++)if(tuple[lane]!=null)mask|=1<<lane;
                var command=Instruction(10,register,mask);
                for(int lane=0;lane<width;lane++)if(tuple[lane] is Store s)
                {int p=5+5*lane;command[p]=s.Op==10?2:3;command[p+1]=s.Buffer;command[p+2]=s.Axis;command[p+3]=s.Mapping;}
            }
            if(dependent)
            {
                // Preserve every original read snapshot and store boundary. Each
                // scalar dependency occupies its actual output axis, with explicit
                // lane transfer when a later expression reads it on another axis.
                // This path is compile-time scheduling, never runtime recognition.
                var saved=new Dictionary<int,int>();var lanes=new Dictionary<int,int>();
                void Assign(Node n,int lane){if(lanes.ContainsKey(n.Id))return;lanes[n.Id]=lane;if(n.Left!=null)Assign(n.Left,lane);if(n.Right!=null)Assign(n.Right,lane);}
                foreach(var s in stores)Assign(s.Value,scalar?0:s.Axis);
                int Operand(Node n,int lane)
                {
                    int source=saved[n.Id];if(lanes[n.Id]==lane)return source;
                    int transferred=registers++;var c=Instruction(3,transferred,1<<lane,source,lanes[n.Id]);c[5]=lane;return transferred;
                }
                foreach(var item in ordered)
                {
                    if(item.Node is Node n)
                    {
                        if(!lanes.ContainsKey(n.Id))continue;int lane=lanes[n.Id];
                        int r=registers++;saved[n.Id]=r;
                        if(n.Op<=3){var command=Instruction(0,r,1<<lane);Descriptor(command,lane,n);}
                        else Instruction(n.Op,r,1<<lane,Operand(n.Left!,lane),n.Right==null?0:Operand(n.Right,lane));
                    }
                    else
                    {
                        var s=item.Store!;int lane=scalar?0:s.Axis;
                        var tuple=new Store?[width];tuple[lane]=s;Output(tuple,Operand(s.Value,lane));
                    }
                }
            }
            else
            {
                // All loads commute with the intervening writes, proven above.
                // Pair outputs by their actual coordinate axes, irrespective of
                // expression shape. Unlike a recognizer this handles any ordered DAG.
                var group=new Store?[width];var outputs=new List<int[]>();
                void Flush(){if(group.All(s=>s==null))return;int r=Pack(group.Select(s=>s?.Value).ToArray());Output(group,r);outputs.Add(code[^1]);code.RemoveAt(code.Count-1);group=new Store?[width];}
                foreach(var s in stores){int axis=scalar?0:s.Axis;if(group[axis]!=null || s.Op==11)Flush();group[axis]=s;if(s.Op==11)Flush();}Flush();
                code.AddRange(outputs);
            }
            // Materialize constant operands as interleaved rows, not runtime axis
            // columns. Each lane keeps its independently measured bit pattern.
            foreach(var command in code.Where(c=>c[0]==0))
            {
                int mask=command[2];bool constants=true;
                for(int lane=0;lane<width;lane++)if((mask&(1<<lane))!=0 && command[5+5*lane]!=0)constants=false;
                if(!constants)continue;
                bool varying=false;for(int lane=0;lane<width;lane++)if((mask&(1<<lane))!=0 && command[7+5*lane]!=0)varying=true;
                if(varying && count<0)throw new InvalidDataException("Unbounded constant row.");
                int offset=values.Count,rows=varying?count:1;
                for(int row=0;row<rows;row++)for(int lane=0;lane<width;lane++)
                    values.Add((mask&(1<<lane))==0?0:floatBits[command[6+5*lane]+row*command[7+5*lane]]);
                command[0]=1;command[3]=offset;command[4]=varying?width:0;
            }
            foreach(var command in code.Where(c=>c[0] is 0 or 10))
            {
                int mask=command[2],buffer=-1,mapping=0;bool records=width==3;
                for(int lane=0;lane<width;lane++)if((mask&(1<<lane))!=0)
                {
                    int p=5+5*lane;
                    if(command[p]!=2 || command[p+2]!=lane || buffer>=0 && (buffer!=command[p+1] || mapping!=command[p+3]))records=false;
                    buffer=command[p+1];mapping=command[p+3];
                }
                if(records){command[0]=command[0]==0?2:11;command[3]=buffer;command[4]=mapping;}
            }
            // General cross-row memory proof, assuming every coordinate binding
            // can alias every other binding. No expression shape is considered.
            bool independent=count>=0 && stores.All(s=>s.Op==10);
            var writers=new Dictionary<(int Vertex,int Axis),int>();
            int Vertex(int mapping,int row)=>mapping== -1?row:mapping>=0?old[mapping+row]:-1;
            foreach(var s in stores)for(int row=0;independent && row<count;row++)
            {
                int vertex=Vertex(s.Mapping,row);var key=(vertex,s.Axis);
                if(vertex<0 || writers.TryGetValue(key,out int previous) && previous!=row)independent=false;
                else writers[key]=row;
            }
            foreach(var n in ordered.Where(x=>x.Node?.Op==2).Select(x=>x.Node!))for(int row=0;independent && row<count;row++)
            {
                int vertex=Vertex(n.C,row);
                if(vertex<0 || writers.TryGetValue((vertex,n.B),out int previous) && previous!=row)independent=false;
            }
            words[3+e]=words.Count;words.AddRange(new[]{count,registers,code.Count,width,independent?512:0});
            int indexStart=words.Count+Words*code.Count;var indices=new List<int>();var map=new Dictionary<int,int>();
            foreach(var command in code)
            {
                int Remap(int originalIndex)
                {
                    if(originalIndex<0)return originalIndex;
                    if(!map.TryGetValue(originalIndex,out int mapped))
                    {mapped=indexStart+indices.Count;map.Add(originalIndex,mapped);indices.AddRange(old.Skip(originalIndex).Take(count));}
                    return mapped;
                }
                if(command[0] is 2 or 11){command[4]=Remap(command[4]);continue;}
                if(command[0] is not (0 or 10))continue;
                // Even a mixed parameter/constant stage stores its constant lanes
                // as XYZ rows. Runtime binding never creates constant axis columns.
                if(command[0]==0)
                {
                    var constants=Enumerable.Range(0,width).Where(l=>(command[2]&(1<<l))!=0 && command[5+5*l]==0).ToArray();
                    if(constants.Length!=0)
                    {
                        bool varying=constants.Any(l=>command[7+5*l]!=0);int rows=varying?count:1,offset=values.Count;
                        if(rows<0)throw new InvalidDataException("Unbounded constant rows.");
                        for(int row=0;row<rows;row++)for(int lane=0;lane<width;lane++)values.Add(constants.Contains(lane)?floatBits[command[6+5*lane]+row*command[7+5*lane]]:0);
                        foreach(int lane in constants){command[6+5*lane]=offset+lane;command[7+5*lane]=varying?width:0;}
                    }
                }
                for(int lane=0;lane<width;lane++)
                {
                    if((command[2]&(1<<lane))==0)continue;int p=5+5*lane,kind=command[p];
                    if(kind is not (2 or 3))continue;
                    int slot=command[0]==10 || kind==2?p+3:p+2;int originalIndex=command[slot];
                    command[slot]=Remap(originalIndex);
                }
            }
            foreach(var command in code)words.AddRange(command);words.AddRange(indices);
        }
        using var stream=new MemoryStream();using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true))
        {
            writer.Write(0x314e4242);writer.Write(1);foreach(int length in new[]{words.Count,0,0,0,values.Count,0})writer.Write(length);
            foreach(int word in words)writer.Write(word);foreach(int bits in values)writer.Write(bits);
        }
        byte[] payload=stream.ToArray();stream.Write(SHA256.HashData(payload));return stream.ToArray();
    }
}
