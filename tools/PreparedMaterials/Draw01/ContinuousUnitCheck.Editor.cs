using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BridgeBuilder.Bridges;

// Private consumer fixture. No generated bridge or installed material is changed.
public static class Draw01ContinuousUnitCheck
{
    public static void Run()
    {
        string Arg(string n)=>Draw01AppearancePartitionCheck.Argument(n);
        var report=JsonUtility.FromJson<PrepareRepeatUnits.Report>(File.ReadAllText(Arg("-unitReport")));
        var manifest=JsonUtility.FromJson<PrepareRepeatUnits.Manifest>(File.ReadAllText(Arg("-unitManifest")));
        var product=report.units[0].channels[0];var unit=manifest.units[0];
        // Native left asphalt faces1512/1513: full finite U, one original4m V cell.
        float u0=(float)product.contentMin[0],u1=(float)product.contentMax[0];
        var nativeSurface=new Colossal.IO.AssetDatabase.SurfaceAsset();
        int checkedVertices=0;
        foreach(float width in new[]{1f,1.7f})foreach(bool right in new[]{false,true})foreach(bool positive in new[]{false,true})
        {
            float finiteWidth=(float)unit.physicalSize[0]*width;
            float xScale=(u1-u0)/finiteWidth*(right?-1:1);
            float sign=positive?1:-1;
            // Continuous V crosses multiple periods. No frac and no period planes.
            var u=new Vector4(xScale,0,0,u1);
            var v=new Vector4(0,0,sign*(right?-1:1)/4f,(right?1:0)+(right?1:-1)*73.9609375f/4f);
            var patch=new MovableExistingRoadSurface.Patch(new Vector2(u0,0),new Vector2(u1,1),u,v,
                nativeSurface,preparedUnit:true,repeatU:false,repeatV:true,projectTangent:true);
            if(!patch.PreparedUnit||patch.SourceUv||patch.SourceAtlasRepeat||patch.RepeatU||!patch.RepeatV||!patch.ProjectTangent)
                throw new Exception("Continuous prepared consumer flags differ.");
            float x=right?finiteWidth:-finiteWidth;
            var positions=new[]{new Vector3(x,0,sign*69.9609375f),new Vector3(0,0,sign*81.9609375f),new Vector3(x,0,sign*81.9609375f)};
            var uv=new Vector2[3];
            for(int i=0;i<3;i++)
            {
                var p=new Vector4(positions[i].x,positions[i].y,positions[i].z,1);
                uv[i]=new Vector2(Vector4.Dot(patch.U,p),Vector4.Dot(patch.V,p));
                if(uv[i].x<u0-1e-6||uv[i].x>u1+1e-6)throw new Exception("Finite U left prepared guard.");
                checkedVertices++;
            }
            if(Math.Abs(Math.Abs(uv[1].y-uv[0].y)-3)>1e-5)throw new Exception("Three periods collapsed by vertex modulo.");
            // The source triangle and its barycentric interpolation stay unchanged.
            var midpoint=(positions[0]+positions[1])*.5f;
            float interpolated=(uv[0].y+uv[1].y)*.5f;
            if(Math.Abs(interpolated-Vector4.Dot(v,new Vector4(midpoint.x,midpoint.y,midpoint.z,1)))>1e-5)
                throw new Exception("Continuous mapping interpolation failed.");
        }
        File.WriteAllText(Arg("-report"),"{\"status\":\"CONSUMER_FIXTURE_PASS\",\"vertices\":"+checkedVertices+",\"inputTriangles\":8,\"outputTriangles\":8,\"newPeriodCuts\":0,\"nativeSamplingAccepted\":false,\"scope\":\"Prepared Patch flags and continuous interpolation on synthetic source-domain triangles; not final Compose or game output\"}");
        Debug.Log("PASS Draw01 continuous prepared consumer fixture");EditorApplication.Exit(0);
    }
}
