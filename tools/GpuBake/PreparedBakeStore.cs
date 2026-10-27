#nullable enable
using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

// Durable offline conversion products. Not a cache of generated bridge results.
// A source/recipe key is resolved before the producer may allocate GPU resources.
internal sealed class PreparedBakeStore
{
    internal readonly struct Payload
    {
        internal readonly string Cid;
        internal readonly byte[] Bytes;
        internal Payload(string cid,byte[] bytes){Cid=cid;Bytes=bytes;}
    }
    private readonly string root,recipe;
    internal int Created { get; private set; }
    internal int Reused { get; private set; }
    internal PreparedBakeStore(string root,string recipe)
    {this.root=Path.GetFullPath(root);this.recipe=recipe;Directory.CreateDirectory(this.root);}

    internal Payload Get(string key,string extension,Func<byte[]> produce)
    {
        if(key.Length!=64 || Array.Exists(key.ToCharArray(),c=>!"0123456789abcdef".Contains(c.ToString())) ||
            (extension!=".Texture" && extension!=".Surface"))throw new ArgumentException("Invalid prepared resource key.");
        string path=Path.Combine(root,key+extension),receipt=path+".sha256";
        byte[] bytes;
        if(File.Exists(path) || File.Exists(receipt))
        {
            // Never hide an incomplete/corrupted source product by rebaking it.
            if(!File.Exists(path) || !File.Exists(receipt))throw new InvalidDataException("Incomplete prepared product: "+path);
            bytes=File.ReadAllBytes(path);
            if(File.ReadAllText(receipt)!=Digest(bytes))throw new InvalidDataException("Prepared product checksum differs: "+path);
            Reused++;
        }
        else
        {
            bytes=produce();
            if(bytes==null || bytes.Length==0)throw new InvalidDataException("Empty prepared product: "+key);
            string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                File.WriteAllBytes(temporary,bytes);File.Move(temporary,path);
                File.WriteAllText(temporary,Digest(bytes),new UTF8Encoding(false));File.Move(temporary,receipt);
            }
            finally {if(File.Exists(temporary))File.Delete(temporary);}
            Created++;
        }
        return new Payload(Digest(Encoding.UTF8.GetBytes(recipe+"/"+Digest(bytes))).Substring(0,32),bytes);
    }

    // The caller must establish equivalence from source/recipe inputs, never by
    // rebaking and comparing outputs. Aliases point directly to validated products.
    internal Payload GetEquivalent(string key,string legacyKey,string extension,string[] readOnly,Func<byte[]> produce)
    {
        bool Valid(string value)=>value.Length==64 && !Array.Exists(value.ToCharArray(),c=>!"0123456789abcdef".Contains(c.ToString()));
        if(!Valid(key)||!Valid(legacyKey)||(extension!=".Texture"&&extension!=".Surface"))
            throw new ArgumentException("Invalid equivalent product key.");
        var roots=new string[readOnly.Length+1];roots[0]=root;
        for(int i=0;i<readOnly.Length;i++)roots[i+1]=Path.GetFullPath(readOnly[i]);
        byte[] Merge(byte[]? previous,byte[] next)
        {
            if(previous!=null && Digest(previous)!=Digest(next))throw new InvalidDataException("Equivalent prepared products differ: "+key);
            return next;
        }
        byte[]? Direct(string requested)
        {
            byte[]? found=null;
            foreach(string folder in roots)
            {
                string path=Path.Combine(folder,requested+extension),receipt=path+".sha256";
                if(!File.Exists(path)&&!File.Exists(receipt))continue;
                if(!File.Exists(path)||!File.Exists(receipt))throw new InvalidDataException("Incomplete prepared product: "+path);
                var bytes=File.ReadAllBytes(path);
                if(File.ReadAllText(receipt)!=Digest(bytes))throw new InvalidDataException("Prepared product checksum differs: "+path);
                found=Merge(found,bytes);
            }
            return found;
        }
        var canonical=Direct(key);
        bool aliased=false;
        foreach(string folder in roots)
        {
            string alias=Path.Combine(folder,key+extension+".alias");
            if(!File.Exists(alias))continue;
            var parts=File.ReadAllText(alias).Split('\n');
            if(parts.Length!=2||!Valid(parts[0])||!Valid(parts[1])||parts[0]==key)
                throw new InvalidDataException("Invalid prepared alias: "+alias);
            var bytes=Direct(parts[0]); // No alias recursion or path-based target.
            if(bytes==null||Digest(bytes)!=parts[1])throw new InvalidDataException("Missing or changed prepared alias target: "+alias);
            canonical=Merge(canonical,bytes);aliased=true;
        }
        var legacy=Direct(legacyKey);
        if(canonical!=null && legacy!=null)Merge(canonical,legacy);
        var result=canonical??legacy;
        if(result==null)return Get(key,extension,produce);
        if(canonical==null && !aliased)
        {
            string alias=Path.Combine(root,key+extension+".alias");
            string temporary=alias+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                File.WriteAllText(temporary,legacyKey+"\n"+Digest(result),new UTF8Encoding(false));
                File.Move(temporary,alias);
            }
            finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
        Reused++;
        return new Payload(Digest(Encoding.UTF8.GetBytes(recipe+"/"+Digest(result))).Substring(0,32),result);
    }

    internal static string Key(Action<BinaryWriter> write)
    {using var bytes=new MemoryStream();using var writer=new BinaryWriter(bytes);write(writer);writer.Flush();return Digest(bytes.ToArray());}
    private static string Digest(byte[] bytes)
    {using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
}
