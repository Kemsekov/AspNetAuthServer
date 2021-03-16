using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WebApi.Contexts;
using WebApi.Entities;
using System.Linq;

namespace WebApi.Models.Requests
{
    public class FindUser
    {
        public string id{get;set;}
        [EmailAddress]
        public string email{get;set;}
        [MinLength(4)]
        [MaxLength(256)]
        public string name{get;set;}
        [Phone]
        public string phoneNumber{get;set;}
        [MinLength(5)]
        [MaxLength(256)]
        public string userName{get;set;}
        [JsonIgnore]
        public List<string> Errors{get;protected set;} = new List<string>();
        /// <summary>
        /// Will try to find user by email, name, phoneNumber or userName. If there is multiple
        /// params or some other error then returns null and fill Errors List with errors.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns>IQueryable that can be used to receive user by calling on it FirstOrDefault or FirstOrDefaultAsync.</returns>
        public IQueryable<ApplicationUser> GetUser(WebApiDbContext dbContext){
  
            //this is cringe but... yes...
            var isFound = email!=null ? 1:0 + phoneNumber!=null ? 1:0 + name!=null ? 1:0 + userName!=null ? 1:0 + id!=null ? 1:0;
            
            if(isFound!=1){
                Errors.Add(
                    @"findUser body should contain one of following params : 'userName' | 'email' | 'phoneNumber' | 'name' |'id'"
                );
                return null;
            }
            email = email?.ToUpper();
            userName = userName?.ToUpper();
            return dbContext.Users.Where(u=>
                !string.IsNullOrEmpty(email)       ? u.NormalizedEmail    == email      : true &&
                !string.IsNullOrEmpty(name)        ? u.Name               == name       : true &&
                !string.IsNullOrEmpty(phoneNumber) ? u.PhoneNumber        == phoneNumber: true &&
                !string.IsNullOrEmpty(userName)    ? u.NormalizedUserName == userName   : true &&
                !string.IsNullOrEmpty(id)          ? u.Id                 == id         : true
                );
        }
    }
}