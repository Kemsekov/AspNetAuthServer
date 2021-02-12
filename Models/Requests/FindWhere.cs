using System;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests
{
    public class FindWhereRequest
    {
        [MinLength(5)]
        public string UserName{get;set;}
        [EmailAddress]
        public string Email{get;set;}
        public string Id{get;set;}
    }
}