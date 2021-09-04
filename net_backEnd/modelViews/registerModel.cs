using System;
using System.ComponentModel.DataAnnotations;

namespace net_backEnd.modelViews
{
    public class RegisterModel
    {
        [StringLength(256),Required,DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [StringLength(256), Required]
        public string UserName { get; set; }

        [ Required]
        public string Password { get; set; }
    }
}
