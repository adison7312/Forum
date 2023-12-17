using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication17.Models
{
    public class MessageUser
    {
        public int MessageId { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public virtual ApplicationUser Sender { get; set; }
        public virtual Message Message { get; set; }
    }
}