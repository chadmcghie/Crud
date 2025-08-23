using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    internal class Person
    {
        public string FullName { get; set; }
        public PhoneAttribute Phone { get; set; }

    }
}
