

using MacroBackendTest_Backend.Models;
using MacroBackendTest_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace TestBackendRepository;

public class RepositoryTests
{

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestSequence()
    {
        // we want "brand-new", empty repository here
        PersonsRepository _repository = new PersonsRepository(new MyDbContext("TestSequence"), true);

        int count = _repository.Count();
        Assert.That(count, Is.EqualTo(0), "Repository should be empty");

        _repository.InitWithDefault();
        count = _repository.Count();
        Assert.That(count, Is.EqualTo(1), $"Repository should have one element now, but count={count}");

        Person testDefault = _repository.GetTestDefaultPerson();

        Person? person = _repository.GetPerson(testDefault.ID);
        Assert.That(person, Is.Not.Null, $"Cannot get test item by ID");

        person = _repository.FindPerson(testDefault.FirstName, testDefault.LastName, testDefault.DateOfBirth);
        Assert.That(person, Is.Not.Null, $"Cannot get test item by name/birth");

        Person person2 = person.Clone();
        person2.StreetName = "new street name";
        _repository.UpdatePerson(person2);

        person = _repository.GetPerson(person2.ID);
        Assert.That(person, Is.Not.Null, $"Cannot get test item by ID");
        Assert.That(person.StreetName, Is.EqualTo(person2.StreetName), "Street name is not changed?");

        bool ret = _repository.DeletePerson(person2.ID);
        Assert.That(ret, Is.True, "Cannot delete person");

        count = _repository.Count();
        Assert.That(count, Is.EqualTo(0), "Seems like person was not deleted");

        person = _repository.FindPerson(testDefault.FirstName, testDefault.LastName, testDefault.DateOfBirth);
        Assert.That(person, Is.Null, "Deleted item should not be found");

        Assert.Pass();
    }

    [Test]
    public void TestTwoItems()
    {
        // we want "brand-new", empty repository here
        PersonsRepository _repository = new PersonsRepository(new MyDbContext("TestTwoItems"), true);

        int count = _repository.Count();
        Assert.That(count, Is.EqualTo(0), "Repository should be empty");

        Person testDefault = _repository.GetTestDefaultPerson();
        int id = _repository.AddPerson(testDefault);
        Assert.That(id, Is.GreaterThan(0), "new item ID should be > 0");

        count = _repository.Count();
        Assert.That(count, Is.EqualTo(1), $"we should have 1 item now, not {count}");

        Person test2 = testDefault.Clone();
        test2.LastName = "Second Last Name";
        int id2 = _repository.AddPerson(test2);
        Assert.That(id2, Is.GreaterThan(0), "new item ID should be > 0");

        count = _repository.Count();
        Assert.That(count, Is.EqualTo(2), $"we should have 2 items now, not {count}");

        var list = _repository.GetPageOfResults(0);
        Assert.That(list.Count(), Is.EqualTo(2), $"we should get 2 items in results Page");

        Assert.Pass();
    }

    [Test]
    public void TestRODOblock()
    {
        // we want "brand-new", empty repository here
        PersonsRepository _repository = new PersonsRepository(new MyDbContext("TestRODOblock"), true);

        int count = _repository.Count();
        Assert.That(count, Is.EqualTo(0), "Repository should be empty");

        Person testDefault = _repository.GetTestDefaultPerson();
        int id = _repository.AddPerson(testDefault);
        Assert.That(id, Is.GreaterThan(0), "new item ID should be > 0");

        count = _repository.Count();
        Assert.That(count, Is.EqualTo(1), $"we should have 1 item now, not {count}");

        bool ret = _repository.SetRODO(id);
        Assert.That(ret, Is.True, "Cannot RODO-block person");

        count = _repository.Count();
        Assert.That(count, Is.EqualTo(0), $"we should have 0 item now, not {count}");

        Assert.Pass();
    }


}