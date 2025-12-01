using System;

namespace Assets.Scripts
{
    /// <summary>
    /// Represents a single message in the chat conversation
    /// </summary>
    [Serializable]
    public class GeminiChatMessage
    {
        public enum Role
        {
            User,
            Model
        }
        
        public Role role;
        public string content;
        public DateTime timestamp;
        
        public GeminiChatMessage(Role role, string content)
        {
            this.role = role;
            this.content = content;
            this.timestamp = DateTime.Now;
        }
        
        public string GetRoleString()
        {
            return role == Role.User ? "user" : "model";
        }
    }
}
