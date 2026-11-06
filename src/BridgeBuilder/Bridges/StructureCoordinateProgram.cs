using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Packed4 = System.Numerics.Vector4;

namespace BridgeBuilder.Bridges;

// Offline-scheduled packed XYZ stages. There is no runtime expression recognition.
internal sealed class StructureCoordinateProgram
{
    private const int Magic = 0x32465041, InstructionWords = 20;
    private readonly HardcodedNumbers _data;
    private readonly Entry[] _entries;
    private static ConditionalWeakTable<HardcodedNumbers, StructureCoordinateProgram> Programs => GenerationParameters.Get<ConditionalWeakTable<HardcodedNumbers, StructureCoordinateProgram>>("BridgeBuilder.Bridges.StructureCoordinateProgram.Programs", () => new());
    private sealed class Entry
    {
        internal readonly int Count, Registers, Width, BlockRows;
        internal readonly int[][] Code;
        internal readonly int[] Writes;
        internal readonly bool[] Initialize;
        internal readonly HashSet<int> Outputs = new();
        internal Entry(HardcodedNumbers data,int offset)
        {
            int[] words=data.I32;
            if(offset<3+words[2] || offset>words.Length-5)throw new InvalidDataException("Invalid structure entry offset.");
            Count=words[offset];Registers=words[offset+1];int instructions=words[offset+2];Width=words[offset+3];BlockRows=words[offset+4];offset+=5;
            if(Count< -1 || Registers<0 || instructions<0 || Count!=0 && (Registers==0 || instructions==0) || Width is not (1 or 3) || BlockRows is not (0 or 512) || Count<0 && BlockRows!=0 || offset+(long)instructions*InstructionWords>words.Length)
                throw new InvalidDataException("Invalid packed structure entry.");
            Code=new int[instructions][];var defined=new int[Registers];Writes=new int[Registers];Initialize=new bool[instructions];
            void Indices(int start)
            {
                if(start==int.MinValue || start>=0 && (Count<0 || (long)start+Count>words.Length))throw new InvalidDataException("Invalid structure index range.");
                if(start>=0)for(int row=0;row<Count;row++)if(words[start+row]<0)throw new InvalidDataException("Negative structure vertex index.");
            }
            void Values(int start,int step,int lanes)
            {
                if(start<0 || step<0 || Count<0 && step!=0 || (Count==0?start>data.F32.Length:start+(long)Math.Max(0,Count-1)*step+lanes>data.F32.Length))
                    throw new InvalidDataException("Invalid packed coefficient range.");
            }
            for(int i=0;i<instructions;i++)
            {
                var c=new int[InstructionWords];Array.Copy(words,offset+i*InstructionWords,c,0,InstructionWords);Code[i]=c;
                int op=c[0],dst=c[1],mask=c[2];
                if(op<0 || op>12 || dst<0 || dst>=Registers || mask<=0 || (mask>>Width)!=0)
                    throw new InvalidDataException("Invalid packed instruction.");
                bool Register(int r)=>r>=0 && r<Registers && (defined[r]&mask)==mask;
                if(op is >=4 and <=9 or 12)
                {if(!Register(c[3]) || op<=7 && !Register(c[4]))throw new InvalidDataException("Undefined packed operand.");}
                else if(op==1)Values(c[3],c[4],Width);
                else if(op is 2 or 11)
                {
                    if(Width!=3 || c[3]<0 || op==11 && !Register(dst))throw new InvalidDataException("Invalid packed record binding.");
                    Indices(c[4]);if(op==11)Outputs.Add(c[3]);
                }
                else if(op==3)
                {
                    if(c[3]<0 || c[3]>=Registers || c[4]<0 || c[4]>=Width || c[5]<0 || c[5]>=Width || mask!=1<<c[5] || (defined[c[3]]&(1<<c[4]))==0)
                        throw new InvalidDataException("Invalid packed lane transfer.");
                }
                else if(op is 0 or 10)
                {
                    if(op==10 && !Register(dst))throw new InvalidDataException("Undefined packed output.");
                    for(int lane=0;lane<Width;lane++)
                    {
                        if((mask&(1<<lane))==0)continue;
                        int p=5+5*lane,kind=c[p],a=c[p+1],b=c[p+2],map=c[p+3];
                        if(a<0 || kind<0 || kind>3 || op==10 && kind<2 || kind==2 && (b<0 || b>2))throw new InvalidDataException("Invalid packed binding.");
                        if(kind==0)Values(a,b,1);
                        if(kind==2 || kind==3 && op==10)Indices(map);
                        else if(kind==3)Indices(b);
                        if(op==10 && kind==2)Outputs.Add(a);
                    }
                }
                if(op is not (10 or 11)){Initialize[i]=defined[dst]==0;defined[dst]|=mask;Writes[dst]++;}
            }
        }
    }
    private static class CoordinateLayout<T> where T:struct
    {
        internal static readonly bool Valid=Check();
        private static bool Check()
        {
            var fields=typeof(T).GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
            if(fields.Length!=3 || Marshal.SizeOf<T>()!=12)return false;
            var offsets=new HashSet<long>();
            foreach(var field in fields){if(field.FieldType!=typeof(float))return false;offsets.Add(Marshal.OffsetOf<T>(field.Name).ToInt64());}
            return offsets.SetEquals(new long[]{0,4,8});
        }
    }
    private StructureCoordinateProgram(HardcodedNumbers data)
    {
        _data=data;int[] words=data.I32;
        if(words.Length<3 || words[0]!=Magic || words[1]!=2 || words[2]<=0 || words[2]>words.Length-3)
            throw new InvalidDataException("Unsupported packed structure program.");
        _entries=new Entry[words[2]];for(int i=0;i<_entries.Length;i++)_entries[i]=new Entry(data,words[3+i]);
    }
    internal static void Execute<T>(HardcodedNumbers program,int entry,T[][] coordinates,float[][] scalars,float[] parameters,int first=0,int count=-1,int[][]? indices=null) where T:struct
    {
        if(coordinates.Length!=0 && !CoordinateLayout<T>.Valid)throw new ArgumentException("Structure coordinates require packed XYZ floats.");
        Programs.GetValue(program,data=>new StructureCoordinateProgram(data)).Execute(entry,coordinates,scalars,parameters,first,count,indices);
    }
    private int Index(int offset,int row,int first,int[][]? indices)
        =>offset>=0?_data.I32[offset+row]:offset== -1?first+row:indices![-2-offset][first+row];
    private void Execute<T>(int index,T[][] coordinates,float[][] scalars,float[] parameters,int first,int count,int[][]? indices) where T:struct
    {
        var entry=_entries[index];
        if(entry.Count<0)
        {if(first<0 || count<0 || (long)first+count>int.MaxValue)throw new ArgumentOutOfRangeException(nameof(count));}
        else{if(first!=0 || count!= -1)throw new ArgumentException("Measured structure entries have fixed row ranges.");count=entry.Count;}
        if(count==0)return;
        int total=count,capacity=entry.BlockRows==0?count:Math.Min(count,entry.BlockRows);
        int stride=checked((checked(capacity*entry.Width)+3)/4*4),length=0;
        var buffers=GenerationParameters.Get("StructureCoordinateProgram.Buffers", () => ArrayPool<float>.Create());
        float[] storage=buffers.Rent(checked(stride*entry.Registers));
        var direct=new int[entry.Registers];var starts=new int[entry.Registers];
        Span<float> Register(int r)=>direct[r]==0?storage.AsSpan(r*stride,length):MemoryMarshal.Cast<T,float>(coordinates[direct[r]-1].AsSpan()).Slice(starts[r]*3,length);
        try
        {
          for(int baseRow=0;baseRow<total;baseRow+=capacity)
          {
            count=Math.Min(capacity,total-baseRow);length=checked(count*entry.Width);
            Array.Clear(direct,0,direct.Length);
            for(int instruction=0;instruction<entry.Code.Length;instruction++)
            {
                var c=entry.Code[instruction];
                var destination=Register(c[1]);int mask=c[2];
                if(entry.Initialize[instruction] && c[0] is 0 or 1 or 3)destination.Clear();
                switch(c[0])
                {
                    case 0:
                        for(int lane=0;lane<entry.Width;lane++)
                        {
                            if((mask&(1<<lane))==0)continue;int p=5+5*lane,a=c[p+1],b=c[p+2];
                            switch(c[p])
                            {
                                case 0:for(int row=0;row<count;row++)destination[row*entry.Width+lane]=_data.F32[a+(baseRow+row)*b];break;
                                case 1:{float value=parameters[a];for(int row=0;row<count;row++)destination[row*entry.Width+lane]=value;break;}
                                case 2:{var source=MemoryMarshal.Cast<T,float>(coordinates[a].AsSpan());for(int row=0;row<count;row++)destination[row*entry.Width+lane]=source[3*Index(c[p+3],baseRow+row,first,indices)+b];break;}
                                case 3:{var source=scalars[a];for(int row=0;row<count;row++)destination[row*entry.Width+lane]=source[Index(b,baseRow+row,first,indices)];break;}
                            }
                        }
                        break;
                    case 1:
                        if(c[4]==entry.Width && (entry.Writes[c[1]]==1 || mask==(1<<entry.Width)-1))_data.F32.AsSpan(c[3]+baseRow*c[4],length).CopyTo(destination);
                        else for(int lane=0;lane<entry.Width;lane++)if((mask&(1<<lane))!=0)
                        {for(int row=0;row<count;row++)destination[row*entry.Width+lane]=_data.F32[c[3]+(baseRow+row)*c[4]+lane];}
                        break;
                    case 2:
                    {
                        var source=MemoryMarshal.Cast<T,float>(coordinates[c[3]].AsSpan());
                        bool aliases=false;foreach(int output in entry.Outputs)if(ReferenceEquals(coordinates[c[3]],coordinates[output])){aliases=true;break;}
                        if(c[4]== -1 && entry.Writes[c[1]]==1 && !aliases)
                        {direct[c[1]]=c[3]+1;starts[c[1]]=first+baseRow;break;}
                        if(entry.Writes[c[1]]>1 && mask!=7)
                        {
                            for(int row=0;row<count;row++)
                            {int from=Index(c[4],baseRow+row,first,indices)*3,to=row*3;
                             if((mask&1)!=0)destination[to]=source[from];if((mask&2)!=0)destination[to+1]=source[from+1];if((mask&4)!=0)destination[to+2]=source[from+2];}
                        }
                        else if(c[4]== -1)source.Slice((first+baseRow)*3,length).CopyTo(destination);
                        else for(int row=0;row<count;row++)
                        {int from=Index(c[4],baseRow+row,first,indices)*3,to=row*3;destination[to]=source[from];destination[to+1]=source[from+1];destination[to+2]=source[from+2];}
                        break;
                    }
                    case 3:
                    {
                        var source=Register(c[3]);for(int row=0;row<count;row++)destination[row*entry.Width+c[5]]=source[row*entry.Width+c[4]];
                        break;
                    }
                    case 10:
                        for(int row=0;row<count;row++)for(int lane=0;lane<entry.Width;lane++)
                        {
                            if((mask&(1<<lane))==0)continue;int p=5+5*lane,a=c[p+1],b=c[p+2],target=Index(c[p+3],baseRow+row,first,indices);
                            if(c[p]==2)MemoryMarshal.Cast<T,float>(coordinates[a].AsSpan())[3*target+b]=destination[row*entry.Width+lane];
                            else scalars[a][target]=destination[row*entry.Width+lane];
                        }
                        break;
                    case 11:
                    {
                        var target=MemoryMarshal.Cast<T,float>(coordinates[c[3]].AsSpan());
                        if(c[4]== -1 && mask==7)destination.CopyTo(target.Slice((first+baseRow)*3,length));
                        else for(int row=0;row<count;row++)
                        {
                            int vertex=Index(c[4],baseRow+row,first,indices)*3;
                            if((mask&1)!=0)target[vertex]=destination[row*3];
                            if((mask&2)!=0)target[vertex+1]=destination[row*3+1];
                            if((mask&4)!=0)target[vertex+2]=destination[row*3+2];
                        }
                        break;
                    }
                    default:
                        Arithmetic(c[0],destination,Register(c[3]),c[0]<=7?Register(c[4]):default,length,entry.Width,mask,entry.Writes[c[1]]==1);
                        break;
                }
            }
          }
        }
        finally{buffers.Return(storage);}
    }
    // Adjacent SIMD lanes are adjacent XYZ coordinates. Dispatch occurs once per
    // complete stage, without inspecting numerical expression shapes.
    private static void Arithmetic(int op,Span<float> output,ReadOnlySpan<float> a,ReadOnlySpan<float> b,int length,int width,int mask,bool fresh)
    {
        var left=MemoryMarshal.Cast<float,Packed4>(a);var right=op<=7?MemoryMarshal.Cast<float,Packed4>(b):default;
        int blocks=length/4;bool complete=fresh || mask==(1<<width)-1;
        if(complete && (op!=7 || mask==(1<<width)-1))
        {
            var result=MemoryMarshal.Cast<float,Packed4>(output);
            switch(op)
            {
                case 4:for(int i=0;i<blocks;i++)result[i]=left[i]+right[i];break;
                case 5:for(int i=0;i<blocks;i++)result[i]=left[i]-right[i];break;
                case 6:for(int i=0;i<blocks;i++)result[i]=left[i]*right[i];break;
                case 7:for(int i=0;i<blocks;i++)result[i]=left[i]/right[i];break;
                case 8:for(int i=0;i<blocks;i++)result[i]=-left[i];break;
                default:left.CopyTo(result);break;
            }
        }
        else
        switch(op)
        {
            case 4:for(int i=0;i<blocks;i++)Write(output,i*4,left[i]+right[i],width,mask,complete);break;
            case 5:for(int i=0;i<blocks;i++)Write(output,i*4,left[i]-right[i],width,mask,complete);break;
            case 6:for(int i=0;i<blocks;i++)Write(output,i*4,left[i]*right[i],width,mask,complete);break;
            case 7:for(int i=0;i<blocks;i++)
                {var divisor=right[i];if(mask!=(1<<width)-1){int start=i*4;for(int k=0;k<4;k++)if((mask&(1<<((start+k)%width)))==0)Set(ref divisor,k,1f);}
                 Write(output,i*4,left[i]/divisor,width,mask,complete);}break;
            case 8:for(int i=0;i<blocks;i++)Write(output,i*4,-left[i],width,mask,complete);break;
            case 9:case 12:for(int i=0;i<blocks;i++)Write(output,i*4,left[i],width,mask,complete);break;
        }
        for(int i=blocks*4;i<length;i++)if((mask&(1<<(i%width)))!=0)
            output[i]=op switch{4=>a[i]+b[i],5=>a[i]-b[i],6=>a[i]*b[i],7=>a[i]/b[i],8=>-a[i],_=>a[i]};
    }
    private static void Write(Span<float> target,int first,Packed4 value,int width,int mask,bool complete)
    {
        if(complete){MemoryMarshal.Cast<float,Packed4>(target.Slice(first,4))[0]=value;return;}
        if((mask&(1<<(first%width)))!=0)target[first]=value.X;
        if((mask&(1<<((first+1)%width)))!=0)target[first+1]=value.Y;
        if((mask&(1<<((first+2)%width)))!=0)target[first+2]=value.Z;
        if((mask&(1<<((first+3)%width)))!=0)target[first+3]=value.W;
    }
    private static void Set(ref Packed4 value,int lane,float component)
    {switch(lane){case 0:value.X=component;break;case 1:value.Y=component;break;case 2:value.Z=component;break;case 3:value.W=component;break;}}
}
