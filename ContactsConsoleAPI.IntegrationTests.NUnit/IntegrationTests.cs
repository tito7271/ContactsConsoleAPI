﻿using ContactsConsoleAPI.Business;
using ContactsConsoleAPI.Business.Contracts;
using ContactsConsoleAPI.Data.Models;
using ContactsConsoleAPI.DataAccess;
using ContactsConsoleAPI.DataAccess.Contrackts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactsConsoleAPI.IntegrationTests.NUnit
{
    public class IntegrationTests
    {
        private TestContactDbContext dbContext;
        private IContactManager contactManager;

        [SetUp]
        public void SetUp()
        {
            this.dbContext = new TestContactDbContext();
            this.contactManager = new ContactManager(new ContactRepository(this.dbContext));
        }


        [TearDown]
        public void TearDown()
        {
            this.dbContext.Database.EnsureDeleted();
            this.dbContext.Dispose();
        }


        //positive test
        [Test]
        public async Task AddContactAsync_ShouldAddNewContact()
        {
            var newContact = new Contact()
            {
                FirstName = "TestFirstName",
                LastName = "TestLastName",
                Address = "Anything for testing address",
                Contact_ULID = "1ABC23456HH", //must be minimum 10 symbols - numbers or Upper case letters
                Email = "test@gmail.com",
                Gender = "Male",
                Phone = "0889933779"
            };

            await contactManager.AddAsync(newContact);

            var dbContact = await dbContext.Contacts.FirstOrDefaultAsync(c => c.Contact_ULID == newContact.Contact_ULID);

            Assert.NotNull(dbContact);
            Assert.AreEqual(newContact.FirstName, dbContact.FirstName);
            Assert.AreEqual(newContact.LastName, dbContact.LastName);
            Assert.AreEqual(newContact.Phone, dbContact.Phone);
            Assert.AreEqual(newContact.Email, dbContact.Email);
            Assert.AreEqual(newContact.Address, dbContact.Address);
            Assert.AreEqual(newContact.Contact_ULID, dbContact.Contact_ULID);
        }

        //Negative test
        [Test]
        public async Task AddContactAsync_TryToAddContactWithInvalidCredentials_ShouldThrowException()
        {
            var newContact = new Contact()
            {
                FirstName = "TestFirstName",
                LastName = "TestLastName",
                Address = "Anything for testing address",
                Contact_ULID = "1ABC23456HH", //must be minimum 10 symbols - numbers or Upper case letters
                Email = "invalid_Mail", //invalid email
                Gender = "Male",
                Phone = "0889933779"
            };

            var ex = Assert.ThrowsAsync<ValidationException>(async () => await contactManager.AddAsync(newContact));
            var actual = await dbContext.Contacts.FirstOrDefaultAsync(c => c.Contact_ULID == newContact.Contact_ULID);

            Assert.IsNull(actual);
            Assert.That(ex?.Message, Is.EqualTo("Invalid contact!"));

        }

        [Test]
        public async Task DeleteContactAsync_WithValidULID_ShouldRemoveContactFromDb()
        {
            // Arrange
            var newContact = new Contact()
            {
                FirstName = "TestFirstName",
                LastName = "TestLastName",
                Address = "Anything for testing address",
                Contact_ULID = "1ABC23456HH", //must be minimum 10 symbols - numbers or Upper case letters
                Email = "test@gmail.com",
                Gender = "Male",
                Phone = "0889933779"
            };

            await contactManager.AddAsync(newContact);

            // Act
            await contactManager.DeleteAsync(newContact.Contact_ULID);

            // Assert
            var contactInDb = await dbContext.Contacts.FirstOrDefaultAsync(x => x.Contact_ULID == newContact.Contact_ULID);
            Assert.IsNull(contactInDb);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public async Task DeleteContactAsync_TryToDeleteWithNullOrWhiteSpaceULID_ShouldThrowException(string invalidUlid)
        {
            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(async () => await contactManager.DeleteAsync(invalidUlid));
            Assert.That(exception.Message, Is.EqualTo("ULID cannot be empty."));
        }

        [Test]
        public async Task GetAllAsync_WhenContactsExist_ShouldReturnAllContacts()
        {
            // Arrange
            var newContact = new Contact()
            {
                FirstName = "TestFirstName",
                LastName = "TestLastName",
                Address = "Anything for testing address",
                Contact_ULID = "1ABC23456HH", //must be minimum 10 symbols - numbers or Upper case letters
                Email = "test@gmail.com",
                Gender = "Male",
                Phone = "0889933779"
            };

            var secondNewContact = new Contact()
            {
                FirstName = "SecondTestFirstName",
                LastName = "SecondTestLastName",
                Address = "Anything for testing address",
                Contact_ULID = "2ABC23456HH", //must be minimum 10 symbols - numbers or Upper case letters
                Email = "test@gmail.com",
                Gender = "Male",
                Phone = "0889933776"
            };

            await contactManager.AddAsync(newContact);
            await contactManager.AddAsync(secondNewContact);

            // Act
            var result = await contactManager.GetAllAsync();

            // Assert
            var firstContact = result.First();
            Assert.IsNotNull(firstContact);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(firstContact.FirstName, Is.EqualTo(newContact.FirstName));
            Assert.That(firstContact.LastName, Is.EqualTo(newContact.LastName));
            Assert.That(firstContact.Address, Is.EqualTo(newContact.Address));
            Assert.That(firstContact.Contact_ULID, Is.EqualTo(newContact.Contact_ULID));
            Assert.That(firstContact.Email, Is.EqualTo(newContact.Email));
            Assert.That(firstContact.Gender, Is.EqualTo(newContact.Gender));
            Assert.That(firstContact.Phone, Is.EqualTo(newContact.Phone));

            //var secondContact = result.Last();
            var secondContact = result.ToList()[1];

            Assert.IsNotNull(secondContact);
            Assert.That(secondContact.FirstName, Is.EqualTo(secondNewContact.FirstName));
            Assert.That(secondContact.LastName, Is.EqualTo(secondNewContact.LastName));
            Assert.That(secondContact.Address, Is.EqualTo(secondNewContact.Address));
            Assert.That(secondContact.Contact_ULID, Is.EqualTo(secondNewContact.Contact_ULID));
            Assert.That(secondContact.Email, Is.EqualTo(secondNewContact.Email));
            Assert.That(secondContact.Gender, Is.EqualTo(secondNewContact.Gender));
            Assert.That(secondContact.Phone, Is.EqualTo(secondNewContact.Phone));
        }

        [Test]
        public async Task GetAllAsync_WhenNoContactsExist_ShouldThrowKeyNotFoundException()
        {
            // Act & Assert
            var exception = Assert.ThrowsAsync<KeyNotFoundException>(() => contactManager.GetAllAsync());
            Assert.That(exception.Message, Is.EqualTo("No contact found."));
        }

        [Test]
        public async Task SearchByFirstNameAsync_WithExistingFirstName_ShouldReturnMatchingContacts()
        {
            // Arrange
            var newContact = new Contact()
            {
                FirstName = "TestFirstName",
                LastName = "TestLastName",
                Address = "Anything for testing address",
                Contact_ULID = "1ABC23456HH", //must be minimum 10 symbols - numbers or Upper case letters
                Email = "test@gmail.com",
                Gender = "Male",
                Phone = "0889933779"
            };

            var secondNewContact = new Contact()
            {
                FirstName = "SecondTestFirstName",
                LastName = "SecondTestLastName",
                Address = "Anything for testing address",
                Contact_ULID = "2ABC23456HH", //must be minimum 10 symbols - numbers or Upper case letters
                Email = "test@gmail.com",
                Gender = "Male",
                Phone = "0889933776"
            };

            await contactManager.AddAsync(newContact);
            await contactManager.AddAsync(secondNewContact);

            // Act
            var result = await contactManager.SearchByFirstNameAsync(newContact.FirstName);

            // Assert
            var resultContact = result.First();
            Assert.IsNotNull(result);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(resultContact.FirstName, Is.EqualTo(newContact.FirstName));
            Assert.That(resultContact.LastName, Is.EqualTo(newContact.LastName));
            Assert.That(resultContact.Contact_ULID, Is.EqualTo(newContact.Contact_ULID));
        }

        [Test]
        public async Task SearchByFirstNameAsync_WithNonExistingFirstName_ShouldThrowKeyNotFoundException()
        {
            // Act & Assert
            var exception = Assert.ThrowsAsync<KeyNotFoundException>(() => contactManager.SearchByFirstNameAsync("Non Existing Name"));
            Assert.That(exception.Message, Is.EqualTo("No contact found with the given first name."));
        }

        [Test]
        public async Task SearchByLastNameAsync_WithExistingLastName_ShouldReturnMatchingContacts()
        {
            var newContact = new Contact()
            {
                FirstName = "TestFirstName",
                LastName = "TestLastName",
                Address = "Anything for testing address",
                Contact_ULID = "1ABC23456HH", //must be minimum 10 symbols - numbers or Upper case letters
                Email = "test@gmail.com",
                Gender = "Male",
                Phone = "0889933779"
            };

            var secondNewContact = new Contact()
            {
                FirstName = "SecondTestFirstName",
                LastName = "SecondTestLastName",
                Address = "Anything for testing address",
                Contact_ULID = "2ABC23456HH", //must be minimum 10 symbols - numbers or Upper case letters
                Email = "test@gmail.com",
                Gender = "Male",
                Phone = "0889933776"
            };

            await contactManager.AddAsync(newContact);
            await contactManager.AddAsync(secondNewContact);

            // Act
            var result = await contactManager.SearchByLastNameAsync(newContact.LastName);

            // Assert
            var resultContact = result.First();
            Assert.IsNotNull(result);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(resultContact.FirstName, Is.EqualTo(newContact.FirstName));
            Assert.That(resultContact.LastName, Is.EqualTo(newContact.LastName));
            Assert.That(resultContact.Contact_ULID, Is.EqualTo(newContact.Contact_ULID));
        }

        [Test]
        public async Task SearchByLastNameAsync_WithNonExistingLastName_ShouldThrowKeyNotFoundException()
        {
            // Act & Assert
            var exception = Assert.ThrowsAsync<KeyNotFoundException>(() => contactManager.SearchByLastNameAsync("Non Existing Name"));
            Assert.That(exception.Message, Is.EqualTo("No contact found with the given last name."));
        }

        [Test]
        public async Task GetSpecificAsync_WithValidULID_ShouldReturnContact()
        {
            // Arrange
            var newContact = new Contact()
            {
                FirstName = "TestFirstName",
                LastName = "TestLastName",
                Address = "Anything for testing address",
                Contact_ULID = "1ABC23456HH", //must be minimum 10 symbols - numbers or Upper case letters
                Email = "test@gmail.com",
                Gender = "Male",
                Phone = "0889933779"
            };

            await contactManager.AddAsync(newContact);

            // Act
            var result = await contactManager.GetSpecificAsync(newContact.Contact_ULID);

            // Assert
            Assert.NotNull(result);
            Assert.That(result.FirstName, Is.EqualTo(newContact.FirstName));
            Assert.That(result.LastName, Is.EqualTo(newContact.LastName));
            Assert.That(result.Contact_ULID, Is.EqualTo(newContact.Contact_ULID));
        }

        [Test]
        public async Task GetSpecificAsync_WithInvalidULID_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var newContact = new Contact()
            {
                FirstName = "TestFirstName",
                LastName = "TestLastName",
                Address = "Anything for testing address",
                Contact_ULID = "1ABC23456HH", //must be minimum 10 symbols - numbers or Upper case letters
                Email = "test@gmail.com",
                Gender = "Male",
                Phone = "0889933779"
            };

            await contactManager.AddAsync(newContact);

            // Act & Assert
            var exception = Assert.ThrowsAsync<KeyNotFoundException>(() => contactManager.GetSpecificAsync("invalidUlid"));
            Assert.That(exception.Message, Is.EqualTo("No contact found with ULID: invalidUlid"));
        }

        [Test]
        public async Task UpdateAsync_WithValidContact_ShouldUpdateContact()
        {
            // Arrange
            var newContact = new Contact()
            {
                FirstName = "TestFirstName",
                LastName = "TestLastName",
                Address = "Anything for testing address",
                Contact_ULID = "1ABC23456HH", //must be minimum 10 symbols - numbers or Upper case letters
                Email = "test@gmail.com",
                Gender = "Male",
                Phone = "0889933779"
            };

            var secondNewContact = new Contact()
            {
                FirstName = "SecondTestFirstName",
                LastName = "SecondTestLastName",
                Address = "Anything for testing address",
                Contact_ULID = "2ABC23456HH", //must be minimum 10 symbols - numbers or Upper case letters
                Email = "test@gmail.com",
                Gender = "Male",
                Phone = "0889933776"
            };

            await contactManager.AddAsync(newContact);

            newContact.FirstName = "UpdatedFirstName";

            // Act
            await contactManager.UpdateAsync(newContact);

            // Assert
            var contactInDb = await dbContext.Contacts.FirstOrDefaultAsync(x => x.Contact_ULID == newContact.Contact_ULID);
            Assert.NotNull(contactInDb);
            Assert.That(contactInDb.FirstName, Is.EqualTo(newContact.FirstName));
        }

        [Test]
        public async Task UpdateAsync_WithInvalidContact_ShouldThrowValidationException()
        {
            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>(() => contactManager.UpdateAsync(new Contact()));
            Assert.That(exception.Message, Is.EqualTo("Invalid contact!"));
        }
    }
}
