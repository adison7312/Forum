using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Services.Description;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace WebApplication17.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }

        public string Login { get; set; }
        public int Rank { get; set; }
        public string Avatar { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string Privileges { get; set; }
        public virtual ICollection<Moderator> Moderators { get; set; }
        public virtual ICollection<MessageUser> Messages { get; set; }
        public virtual ICollection<Thread> Posts { get; set; }
        public virtual ICollection<Post> Comments { get; set; }

    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Message> Messeges { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Thread> Threads { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Friends> Friends { get; set; }
        public DbSet<News> News { get; set; }
        public DbSet<Moderator> Moderators { get; set; }
        public DbSet<MessageUser> MessageUser { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Friends>()
                .HasRequired(f => f.Friend)
                .WithOptional()
                .WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Friends>()
                .HasRequired(f => f.Friend)
                .WithOptional()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<MessageUser>()
            .HasKey(mu => new { mu.MessageId, mu.ReceiverId, mu.SenderId });

            modelBuilder.Entity<ApplicationUser>()
                        .HasMany(m1 => m1.Messages)
                        .WithRequired()
                        .HasForeignKey(mu => mu.ReceiverId);

            modelBuilder.Entity<ApplicationUser>()
                        .HasMany(m2 => m2.Messages)
                        .WithRequired()
                        .HasForeignKey(mu => mu.SenderId);

            modelBuilder.Entity<Message>()
                        .HasMany(u => u.Users)
                        .WithRequired()
                        .HasForeignKey(mu => mu.MessageId);
        }
    }

    public class IdentityManager
    {
        public RoleManager<IdentityRole> LocalRoleManager
        {
            get
            {
                return new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
            }
        }


        public UserManager<ApplicationUser> LocalUserManager
        {
            get
            {
                return new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext()));
            }
        }


        public ApplicationUser GetUserByID(string userID)
        {
            ApplicationUser user = null;
            UserManager<ApplicationUser> um = this.LocalUserManager;

            user = um.FindById(userID);

            return user;
        }



        public ApplicationUser GetUserByName(string email)
        {
            ApplicationUser user = null;
            UserManager<ApplicationUser> um = this.LocalUserManager;

            user = um.FindByName(email);

            return user;
        }


        public bool RoleExists(string name)
        {
            var rm = LocalRoleManager;

            return rm.RoleExists(name);
        }

        public bool isUserInRole(string userId, string role)
        {
            var um = this.LocalUserManager;
            return um.IsInRoleAsync(userId, role).Result;
        }


        public bool CreateRole(string name)
        {
            var rm = LocalRoleManager;
            var idResult = rm.Create(new IdentityRole(name));

            return idResult.Succeeded;
        }


        public bool CreateUser(ApplicationUser user, string password)
        {
            var um = LocalUserManager;
            var idResult = um.Create(user, password);

            return idResult.Succeeded;
        }


        public bool AddUserToRole(string userId, string roleName)
        {
            var um = LocalUserManager;
            var idResult = um.AddToRole(userId, roleName);

            return idResult.Succeeded;
        }
        public bool isAdmin(ApplicationUser user)
        {
            var rm = LocalUserManager;

            return rm.IsInRole(user.Id, "Admin"); ;
        }


        public bool AddUserToRoleByUsername(string username, string roleName)
        {
            var um = LocalUserManager;

            string userID = um.FindByName(username).Id;
            var idResult = um.AddToRole(userID, roleName);

            return idResult.Succeeded;
        }


        public void ClearUserRoles(string userId)
        {
            var um = LocalUserManager;
            var user = um.FindById(userId);
            var currentRoles = new List<IdentityUserRole>();

            currentRoles.AddRange(user.Roles);

            foreach (var role in currentRoles)
            {
                um.RemoveFromRole(userId, role.RoleId);
            }
        }

        public bool ClearUserFromRole(string userId, string role)
        {
            var um = LocalUserManager;
            var result = um.RemoveFromRoleAsync(userId, role);
            return result.Result.Succeeded;
        }
    }
}