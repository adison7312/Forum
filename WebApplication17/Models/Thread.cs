using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication17.Models
{
    public class Thread
    {
        public int Id { get; set; }
        [Required]
        public string Content { get; set; }
        [Required]
        [StringLength(70)]
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public int ViewCount { get; set; }
        public bool IsPinned { get; set; }
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
        public int SubjectId { get; set; }
        public virtual Subject Subject { get; set; }
        public virtual ICollection<Post> Posts { get; set; }
    }
}