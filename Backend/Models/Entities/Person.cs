
using System.ComponentModel.DataAnnotations;

namespace MacroBackendTest_Backend.Models.Entities
{
    public class Person
    {
        // from specification; assuming all fields wihout "optional" are required
        [Required]
        public string FirstName { get; set; } = "";
        [Required]
        public string LastName { get; set; } = "";
        [Required]
        public string StreetName { get; set; } = "";
        [Required]
        public string HouseNumber { get; set; } = "";// 'number' in name but string, as we could have e.g. "11a"

        public string? ApartmentNumber { get; set; }    // 'number' in name but string, as we could have e.g. "11a"
        [Required]
        public string PostalCode { get; set; } = ""; // we can add RegExp validation for selected country - unknown at this stage
        [Required]
        public string Town { get; set; } = "";
        [Required, Phone]
        public string PhoneNumber { get; set; } = "";
        [Required]
        public DateTime DateOfBirth { get; set; }

        // this is fallback, as calculation of age differs with culture 
        public int Age => DateTime.Now.Year - DateOfBirth.Year;

        // mine

        [Required]
        public int ID { get; set; } = -1;

        // mine - simple additions for 'RODO'; it is not full support of RODO requirements!
        public DateTime deleted { get; set; }

        public DateTime RODOblock { get; set; } // placeholder, allowed by law: "please do not process data for such user"; all data non-required for ID should be emptied/nulled


        public string DumpAsJSON()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
        }

        public bool IsRodoBlocked()
        {
            return RODOblock.Year > 2000;
        }

        public bool IsDeleted()
        {
            return deleted.Year > 2000;
        }

        public Person Clone()
        {
            string sTxt = DumpAsJSON();
            Person? oNew = Newtonsoft.Json.JsonConvert.DeserializeObject<Person>(sTxt);
            if (oNew == null)
                throw new Exception("I cannot be null!");
            return oNew;
        }


    }
}
