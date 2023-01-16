
using Macrix_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using Newtonsoft.Json;

namespace Macrix_Backend.Models
{
    public class MyDbContext : DbContext
    {

        private string _databaseName = "macrix";

        /// <summary>
        /// use with parameter = uniq name, if you want to have concurrent databases
        /// </summary>
        /// <param name="databaseName"></param>
        public MyDbContext(string databaseName = "macrix")
        {
            _databaseName = databaseName;
        }

        public virtual DbSet<Person> Persons { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase(_databaseName);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        private string _JsonFilename = "";

        private string DataFolder()
        {
            // various 
            // return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);    // for user
            return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);  // for app
        }

        public void LoadData()
        {
            // we already read data
            if (_JsonFilename != "") return;

            string foldername = DataFolder();
            _JsonFilename = Path.Combine(foldername, "macrix");
            Directory.CreateDirectory(_JsonFilename);
            _JsonFilename = Path.Combine(_JsonFilename, "database.json");

            if (!File.Exists(_JsonFilename)) return;

            string sTxt = File.ReadAllText(_JsonFilename);
            if (sTxt is null) return;
            if (sTxt.Length < 10) return;

            List<Person>? JsonList;

            try
            {
                JsonList = (List<Person>?)Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, typeof(List<Person>));
            }
            catch
            {
                return;
            }

            if (JsonList is null) return;

            //modelBuilder.Entity<Person>().HasData(new List<Person>() { JsonList };

            foreach (var person in JsonList)
            {

                Persons.Add(person);
            }

            base.SaveChanges();

        }

        public override int SaveChanges()
        {
            int temp = base.SaveChanges();
            SaveJson();
            return temp;
        }

        void SaveJson()
        {
            if (_JsonFilename == "") return;

            var JsonList = new List<Person>();

            foreach (var person in Persons)
            {
                JsonList.Add(person);
            }

            var oSerSet = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore };
            string sTxt = Newtonsoft.Json.JsonConvert.SerializeObject(JsonList, Formatting.Indented, oSerSet);

            File.WriteAllText(_JsonFilename, sTxt);

        }
        ~MyDbContext()
        {
            SaveJson();
        }


    }
}
