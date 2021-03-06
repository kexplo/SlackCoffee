﻿using Microsoft.AspNetCore.Mvc;
using SlackCoffee.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SlackCoffee.Test
{
    class TestUser
    {
        public string Id { get; set; }
        public bool Manager { get; set; }
    }

    class TestCommand : Command<TestUser>
    {
        public readonly bool ForManager;

        public TestCommand(string id, bool forManager) : base(id)
        {
            ForManager = forManager;
        }

        public override bool Authorized(TestUser user)
        {
            return !ForManager || user.Manager;
        }

        public override string MakeDescription()
        {
            return ForManager ? "true" : "false";
        }

        public override int CompareTo(object other)
        {
            return Id.CompareTo(((TestCommand)other).Id);
        }
    }

    class TestCommandAttribute: CommandAttribute
    {
        public TestCommandAttribute(string id, bool forManager)
            : base(new TestCommand(id, forManager))
        { }
    }

    class TestClass
    {
        private static CommandHandlers<TestClass, TestUser> handlers = new CommandHandlers<TestClass, TestUser>();

        [TestCommand("ReturnTypeError", true)]
        public int ReturnTypeErrorHandler(TestUser user, string text)
        {
            return 0;
        }

        [TestCommand("InputTypeError", true)]
        public async Task<IActionResult> InputTypeErrorHandler(int user, string text)
        {
            return null;
        }

        [TestCommand("ParameterCountError", true)]
        public async Task<IActionResult> ParameterCountErrorHandler(TestUser user)
        {
            return null;
        }

        [TestCommand("Manager", true)]
        public async Task<IActionResult> ManagerRequestHandler(TestUser user, string text)
        {
            return new OkResult();
        }

        [TestCommand("Normal", false)]
        public async Task<IActionResult> NormalRequestHandler(TestUser user, string text)
        {
            return new OkResult();
        }

        public async Task<IActionResult> Handle(TestUser user, string command)
        {
            var handler = handlers.GetHandler(this, user, command);
            if (handler == null)
                return new BadRequestResult();
            return await handler("");
        }
    }

    public class CommandTest
    {
        [Fact]
        public async void CommandErrorTest()
        {
            var manager = new TestUser { Id = "user01", Manager = true };
            var user = new TestUser { Id = "user02", Manager = false };
            var t = new TestClass();

            await Assert.ThrowsAsync<CommandHandlerException>(() => t.Handle(manager, "ReturnTypeError"));
            await Assert.ThrowsAsync<CommandHandlerException>(() => t.Handle(manager, "InputTypeError"));
            await Assert.ThrowsAsync<CommandHandlerException>(() => t.Handle(manager, "ParameterCountError"));

            Assert.IsType<OkResult>(await t.Handle(manager, "Manager"));
            Assert.IsType<OkResult>(await t.Handle(manager, "Normal"));

            Assert.IsType<BadRequestResult>(await t.Handle(user, "Manager"));
            Assert.IsType<OkResult>(await t.Handle(user, "Normal"));
        }
    }
}
