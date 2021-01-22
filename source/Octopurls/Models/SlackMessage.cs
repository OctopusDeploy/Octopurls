using System.Collections.Generic;
using Newtonsoft.Json;

namespace Octopurls.Models
{
    public class SlackRichMessage
    {
        [JsonProperty("username")]
        public string Username {get;set;}

        [JsonProperty("attachments")]
        public List<SlackMessage> Attachments {get;set;}
    }

    public class SlackMessage
    {

        [JsonProperty("pretext")]
        public string PreText {get;set;}

        [JsonProperty("fallback")]
        public string Fallback {get;set;}

        [JsonProperty("color")]
        public string Color {get;set;}

        [JsonProperty("fields")]
        public List<Field> Fields {get;set;}
    }

    public class Field
    {
        [JsonProperty("title")]
        public string Title {get;set;}

        [JsonProperty("value")]
        public string Value {get;set;}

        [JsonProperty("short")]
        public bool Short {get;set;}
    }
}