// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Jamboree.Telecomms;
using NUnit.Framework;

namespace Content.Tests.Client._Jamboree.Telecomms;

[TestFixture]
public sealed class NtslEditorHintsTest
{
    private static NtslEditorHints Hints(string code)
    {
        var hints = new NtslEditorHints(new[] { "common", "medical", "security" });
        hints.SetCode(code);
        return hints;
    }

    // The cursor goes at the end of the code.
    [TestCase("x = subs", "tr(")]
    [TestCase("x = substr", "(")]
    [TestCase("x = substr()", null)]
    [TestCase("x = sig.ra", "w")]
    [TestCase("x = channels.med", "ical")]
    [TestCase("x = filter_types.ro", "bot")]
    [TestCase("words.Sw", "ap(")]
    [TestCase("x = s", "")]
    [TestCase("x = sig.", "")]
    [TestCase("blocked = list(); x = blo", "cked")]
    [TestCase("def shout(text, times) { } shou", "t(")]
    [TestCase("def process_signal(message) { x = mes", "sage")]
    [TestCase("x = \"subs", null)]
    [TestCase("// subs", null)]
    [TestCase("    ", null)]
    [TestCase("x = 12", null)]
    [TestCase("x = zzz", null)]
    public void TabFinishesNames(string code, string expected)
    {
        Assert.That(Hints(code).Complete(code.Length), Is.EqualTo(expected));
    }

    [Test]
    public void TabInTheMiddleOfAWordIndents()
    {
        Assert.That(Hints("x = substr").Complete(6), Is.Null);
    }

    [Test]
    public void ChoicesListEveryName()
    {
        Assert.That(Hints("x = s").Choices(5), Does.Contain("substr").And.Contain("signal"));
        Assert.That(Hints("x = substr").Choices(10), Is.Empty);
    }

    [TestCase("x = substr(sig.raw, 2", "substr", 1)]
    [TestCase("x = find(lower(sig.raw", "lower", 0)]
    [TestCase("x = find(lower(sig.raw), ", "find", 1)]
    [TestCase("x = find(sig.raw, \"/lo", "find", 1)]
    [TestCase("x = find(sig.raw, \"a,b\", ", "find", 2)]
    [TestCase("words.Insert(1, ", "Insert", 1)]
    [TestCase("def shout(text, times) { } shout(\"a\", ", "shout", 1)]
    [TestCase("x = substr(sig.raw, (1 + ", "substr", 1)]
    public void HintShowsTheCall(string code, string name, int argument)
    {
        Assert.That(Hints(code).TryGetCall(code.Length, out var foundName, out _, out var foundArgument));
        Assert.That(foundName, Is.EqualTo(name));
        Assert.That(foundArgument, Is.EqualTo(argument));
    }

    [TestCase("if (x")]
    [TestCase("x = substr(a, b); y = ")]
    [TestCase("x = substr(a, // b")]
    public void NoHintOutsideCalls(string code)
    {
        Assert.That(Hints(code).TryGetCall(code.Length, out _, out _, out _), Is.False);
    }

    [Test]
    public void UserFunctionsShowTheirParameters()
    {
        const string code = "def shout(text, times) { } shout(";
        Hints(code).TryGetCall(code.Length, out _, out var parameters, out _);
        Assert.That(parameters, Is.EqualTo(new[] { "text", "times" }));
    }
}
