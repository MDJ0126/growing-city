using UnityEngine;
using UnityEngine.UI;
namespace GrowingCity
{
    // Text objects, fonts and styles are authored in the scene. Only transient reward data/motion changes here.
    public sealed class CityGoldHud
    {
        sealed class Slot {public Text Text;public Vector2 Origin,Start,End;public float Age=2;}
        readonly Slot[] slots;
        readonly RectTransform canvas;
        readonly Text total;
        readonly Vector3 totalScale;
        int next;
        float pulse=1;
        double shown,from;
        long target;
        float countAge=1;
        public CityGoldHud(Text[] labels,Text total)
        {
            this.total=total;if(total){totalScale=total.rectTransform.localScale;canvas=total.canvas.transform as RectTransform;}
            slots=new Slot[labels?.Length??0];for(int i=0;i<slots.Length;i++){slots[i]=new Slot{Text=labels[i],Origin=labels[i]?labels[i].rectTransform.anchoredPosition:Vector2.zero};if(labels[i])labels[i].canvasRenderer.SetAlpha(0);}
        }
        public void Earn(long amount,Vector3 worldPoint,Camera camera)
        {
            if(amount<=0)return;pulse=0;if(slots.Length==0||!canvas||!total)return;
            var slot=slots[next++%slots.Length];if(!slot.Text)return;
            Vector2 position;var screen=camera.WorldToScreenPoint(worldPoint);RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,screen,null,out position);
            var rect=canvas.rect;position.x=Mathf.Clamp(position.x,rect.xMin+130,rect.xMax-130);position.y=Mathf.Clamp(position.y,rect.yMin+190,rect.yMax-150);
            slot.Start=position;slot.End=canvas.InverseTransformPoint(total.rectTransform.position);slot.Age=0;slot.Text.text="+"+amount.ToString("N0")+" G";
        }
        public void Tick(float dt)
        {
            countAge+=dt;float countT=Mathf.Clamp01(countAge/.35f);shown=from+(target-from)*(1-System.Math.Pow(1-countT,3));if(total)total.text=System.Math.Round(shown).ToString("N0");
            pulse+=dt;if(total)total.rectTransform.localScale=totalScale*(1+Mathf.Sin(Mathf.Clamp01(pulse/.4f)*Mathf.PI)*.16f);
            foreach(var slot in slots){if(!slot.Text||slot.Age>=1.25f)continue;slot.Age+=dt;float t=Mathf.Clamp01(slot.Age/1.25f);
                float flight=Mathf.Clamp01((t-.35f)/.65f);slot.Text.rectTransform.anchoredPosition=Vector2.Lerp(slot.Start,slot.End,flight*flight)+Vector2.up*(Mathf.Sin(t*Mathf.PI)*55);
                slot.Text.canvasRenderer.SetAlpha(t<.75f?1:1-(t-.75f)/.25f);
                if(t>=1){slot.Text.rectTransform.anchoredPosition=slot.Origin;slot.Text.canvasRenderer.SetAlpha(0);}
            }
        }
        public void SetTotal(long value){if(value==target)return;from=shown;target=value;countAge=0;}
        public void Reset(){foreach(var s in slots){s.Age=2;if(s.Text){s.Text.canvasRenderer.SetAlpha(0);s.Text.rectTransform.anchoredPosition=s.Origin;}}pulse=1;shown=from=0;target=0;countAge=1;if(total){total.rectTransform.localScale=totalScale;total.text="0";}}
    }
}
