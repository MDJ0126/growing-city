using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GrowingCity
{
    public sealed class CityLanguage : MonoBehaviour
    {
        [Serializable] public sealed class Entry {public string key;public string value;}
        [Serializable] public sealed class Locale {public string code;public string name;public GameObject[] labels;public Entry[] entries;}
        [SerializeField] Locale[] locales;
        [SerializeField] Text selectionText;
        [SerializeField] int preference; // 0 = system; 1..9 = explicit language.
        readonly Dictionary<string,string> strings=new Dictionary<string,string>();
        string systemCode="en";
        public event Action Changed;
        public string Code {get;private set;}="en";
        public string Preference=>preference==0?"system":Code;
        public void Initialize(string requestedSystemCode)
        {
            systemCode=requestedSystemCode;
            preference=Mathf.Clamp(PlayerPrefs.GetInt("GrowingCity.Language",0),0,locales.Length);
            Apply();
        }
        public void RefreshSystem(){if(preference!=0)return;var next=SystemCode();if(next==systemCode)return;systemCode=next;Apply();Changed?.Invoke();}
        public void Cycle(){preference=(preference+1)%(locales.Length+1);PlayerPrefs.SetInt("GrowingCity.Language",preference);PlayerPrefs.Save();Apply();Changed?.Invoke();}
        void Apply()
        {
            if(locales==null||locales.Length==0)return;
            int selected=0;if(preference>0)selected=preference-1;else for(int i=0;i<locales.Length;i++)if(locales[i].code==systemCode){selected=i;break;}
            Code=locales[selected].code;strings.Clear();
            for(int i=0;i<locales.Length;i++){foreach(var label in locales[i].labels)if(label)label.SetActive(i==selected);}
            foreach(var entry in locales[selected].entries)strings.Add(entry.key,entry.value);
            if(selectionText)selectionText.text=(preference==0?Get("system")+" / ":"")+locales[selected].name;
        }
        public string Get(string key)=>strings.TryGetValue(key,out var value)?value:key;
        public string Format(string key,params object[] values)=>string.Format(System.Globalization.CultureInfo.InvariantCulture,Get(key),values);
        public static string SystemCode()
        {
            switch(Application.systemLanguage){case SystemLanguage.Korean:return "ko";case SystemLanguage.Japanese:return "ja";case SystemLanguage.Chinese:case SystemLanguage.ChineseSimplified:return "zh-Hans";case SystemLanguage.ChineseTraditional:return "zh-Hant";case SystemLanguage.German:return "de";case SystemLanguage.French:return "fr";case SystemLanguage.Spanish:return "es";case SystemLanguage.Portuguese:return "pt";default:return "en";}
        }
    }
}
