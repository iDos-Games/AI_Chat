using System.Collections.Generic;
using System;

namespace IDosGames
{
    [Serializable]
    public class ChatMessageContent
    {
        public string type;
        public ChatMessageText text;
    }

    [Serializable]
    public class ChatMessageText
    {
        public string value;
        public List<object> annotations = new List<object>();
    }

    [Serializable]
    public class ChatMessageModel
    {
        public string id;
        public string @object = "thread.message";
        public long created_at;
        public string thread_id;
        public string role;
        public List<ChatMessageContent> content;
        public string assistant_id;
        public string run_id;
        public List<object> attachments = new List<object>();
        public Dictionary<string, object> metadata = new Dictionary<string, object>();
    }

    [Serializable]
    public class ChatThreadModel
    {
        public string id;
        public string @object = "thread";
        public long created_at;
        public Dictionary<string, object> metadata = new Dictionary<string, object>();
        public List<ChatMessageModel> messages = new List<ChatMessageModel>();
    }

    [Serializable]
    public class Serialization<T>
    {
        public Serialization(T target)
        {
            this.target = target;
        }

        public T target;
    }
}
