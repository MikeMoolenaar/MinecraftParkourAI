using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApiAiSDK;
using ApiAiSDK.Model;

namespace MinecraftBot
{
    public class APIAI
    {
        private ApiAi apiAi;
        public List<Tuple<string, Action<AIResponse,string>>> events = new List<Tuple<string, Action<AIResponse, string>>>();
        public Action<AIResponse, string> OnNoEventsFired;
        public APIAI(string clienttoken)
        {
            var config = new AIConfiguration(clienttoken, SupportedLanguage.English);
            apiAi = new ApiAi(config);
        }

        public void AddEvent(string trigger, Action<AIResponse, string> function)
        {
            events.Add(new Tuple<string, Action<AIResponse, string>>(trigger, function));
        }

        public void Interact(string input, string playername)
        {
            AIResponse result = apiAi.TextRequest(input);

            //Events uitvoeren
            bool eventfired = false;
            events.Where(x => x.Item1 == result.Result.Metadata.IntentName)
                .ToList()
                .ForEach(x => {
                    eventfired = true;
                    x.Item2(result, playername);
                });

            if(!eventfired && OnNoEventsFired != null)
            {
                OnNoEventsFired(result, playername);
            }
        }
    }
}
