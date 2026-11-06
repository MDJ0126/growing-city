using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using GrowingCity;
// Isolated API doubles: checks locale state/formatting, not Unity rendering or persistence IO.
namespace UnityEngine {
 public class Object {public static implicit operator bool(Object x)=>x!=null;}
 public class MonoBehaviour:Object{}
 public class SerializeField:Attribute{}
 public class GameObject:Object {public bool active;public void SetActive(bool value){active=value;}}
 public static class Mathf{public static int Clamp(int n,int min,int max)=>Math.Max(min,Math.Min(max,n));}
 public static class PlayerPrefs{static Dictionary<string,int> v=new Dictionary<string,int>();public static int GetInt(string k,int d)=>v.TryGetValue(k,out var n)?n:d;public static void SetInt(string k,int n){v[k]=n;}public static void Save(){} }
 public enum SystemLanguage{English,Korean,Japanese,Chinese,ChineseSimplified,ChineseTraditional,German,French,Spanish,Portuguese,Russian}
 public static class Application{public static SystemLanguage systemLanguage;}
}
namespace UnityEngine.UI{public class Text:UnityEngine.Object{public string text;}}
static class Checks{
 static int passed;static void A(bool v,string label){if(!v)throw new Exception(label);passed++;}
 static void Set(object o,string field,object v)=>o.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
 static void Main(){
 var root=JsonDocument.Parse(File.ReadAllText("Assets/Settings/Localization/languages.json")).RootElement;
 var locales=new List<CityLanguage.Locale>();foreach(var l in root.EnumerateArray()){var entries=new List<CityLanguage.Entry>();foreach(var p in l.GetProperty("strings").EnumerateObject())entries.Add(new CityLanguage.Entry{key=p.Name,value=p.Value.GetString()});locales.Add(new CityLanguage.Locale{code=l.GetProperty("code").GetString(),name=l.GetProperty("name").GetString(),labels=new[]{new UnityEngine.GameObject(),new UnityEngine.GameObject()},entries=entries.ToArray()});}
 foreach(var locale in locales){UnityEngine.PlayerPrefs.SetInt("GrowingCity.Language",0);var lang=new CityLanguage();Set(lang,"locales",locales.ToArray());Set(lang,"selectionText",new UnityEngine.UI.Text());lang.Initialize(locale.code);A(lang.Code==locale.code,"system language");foreach(var l in locales)foreach(var g in l.labels)A(g.active==(l==locale),"native variants exclusive");foreach(var e in locale.entries){var f=lang.Format(e.key,1,2,3);A(!string.IsNullOrEmpty(f)&&!f.Contains("{0}"),"all templates format");}A(lang.Format("city",1,2,3).Contains("3"),"city params");}
 var current=new CityLanguage();Set(current,"locales",locales.ToArray());current.Initialize("unsupported");A(current.Code=="en","unknown fallback English");int events=0;current.Changed+=()=>events++;for(int i=0;i<10;i++)current.Cycle();A(current.Preference=="system"&&events==10,"nine overrides and system cycle");current.Cycle();var reloaded=new CityLanguage();Set(reloaded,"locales",locales.ToArray());reloaded.Initialize("ko");A(reloaded.Code=="en"&&reloaded.Preference=="en","override survives reinit");
 foreach(var pair in new[]{(UnityEngine.SystemLanguage.Korean,"ko"),(UnityEngine.SystemLanguage.Japanese,"ja"),(UnityEngine.SystemLanguage.ChineseSimplified,"zh-Hans"),(UnityEngine.SystemLanguage.ChineseTraditional,"zh-Hant"),(UnityEngine.SystemLanguage.German,"de"),(UnityEngine.SystemLanguage.French,"fr"),(UnityEngine.SystemLanguage.Spanish,"es"),(UnityEngine.SystemLanguage.Portuguese,"pt"),(UnityEngine.SystemLanguage.Russian,"en")}){UnityEngine.Application.systemLanguage=pair.Item1;A(CityLanguage.SystemCode()==pair.Item2,"Unity enum mapping");}
 UnityEngine.PlayerPrefs.SetInt("GrowingCity.Language",0);current.Initialize("en");UnityEngine.Application.systemLanguage=UnityEngine.SystemLanguage.Korean;current.RefreshSystem();A(current.Code=="ko","system refresh on focus");
 var n=new NuclearCooldown();A(n.Ready&&n.Launch(),"initial nuclear ready");A(n.Remaining==100&&!n.Launch(),"100 seconds and duplicate blocked");n.Tick(99.999);A(!n.Ready,"before boundary blocked");n.Tick(.0011);A(n.Ready&&n.Launch(),"100 boundary relaunch");n.Tick(0);A(n.Remaining==100,"pause frozen");n.Tick(1);A(n.Remaining==99,"one real second");n.Tick(1000);A(n.Remaining==0,"no negative time");A(new NuclearCooldown().Ready,"new city reset");foreach(double invalid in new[]{-1,double.NaN,double.PositiveInfinity}){bool rejected=false;try{n.Tick(invalid);}catch(ArgumentOutOfRangeException){rejected=true;}A(rejected,"invalid dt rejected");}
 Console.WriteLine("PASS: "+passed+" isolated locale, formatting, selection and nuclear cooldown assertions. Unity IO/rendering untested.");
 }
}
