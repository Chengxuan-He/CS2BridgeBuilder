using System;
using System.IO;
using System.Reflection;
using UnityEngine;
public static class TwoPeriodCheck {
 public static void Run(){
 var m=typeof(PrepareRepeatUnits).GetMethod("StoragePeriods",BindingFlags.Static|BindingFlags.NonPublic);
 object Call(string json)=>m.Invoke(null,new object[]{JsonUtility.FromJson<PrepareRepeatUnits.Unit>(json)});
 if(!Call("{\"repeatAxes\":\"V\"}").Equals((1,1)))throw new Exception("Default changed");
 if(!Call("{\"repeatAxes\":\"V\",\"twoPeriodAxis\":\"V\"}").Equals((1,2)))throw new Exception("V double");
 foreach(var json in new[]{"{\"repeatAxes\":\"U\",\"twoPeriodAxis\":\"V\"}","{\"twoPeriodAxis\":\"UV\"}","{\"twoPeriodAxis\":\"3\"}","{\"twoPeriodAxis\":\"V\",\"bands\":[{}]}","{\"twoPeriodAxis\":\"V\",\"panels\":[{}]}"}){
 bool fail=false;try{Call(json);}catch(TargetInvocationException e)when(e.InnerException is InvalidDataException){fail=true;}if(!fail)throw new Exception("Bad combination accepted");}
 Debug.Log("PASS default periods and five invalid double-period combinations");
 PrepareRepeatUnits.Run();
 }
}
