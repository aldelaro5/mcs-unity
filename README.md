# mcs-unity

This is an edit of the official Mono.CSharp codebase with edits for Unity modding. It pulls from this commit https://github.com/mono/mono/commit/1ed1688a543c0c03f8fc0cc8e6ca234a6bd45eb0 and contains many changes to make mcs a more self contained repository. It is a completely redone from scratch attempt to redo every modifications people have made in the past in order to fix a bunch of issues with it. It still integrates changes done by the following forks:

- https://github.com/kkdevs/mcs
- https://github.com/ghorsington/mcs-unity
- https://github.com/sinai-dev/mcs-unity

Here are the main changes:

* Add .NET 3.5 support using a bunch of ifdef and MonoMod.Backports
* Force Evaluator to import all memebers for code completion and for compilation
* Ignore access checks during compilation and at runtime with the same behavior that "allow unsafe code" does on Mono
* Let the autocomplete seek way more items such as namespaces and extension methods as well as remove compiler generated items which aren't speakable
* Add case insensitive support for autocompletion
