## Welcome to the CSharpTest.Net code library ##
Project Home: http://csharptest.net

**Binary downloads are no longer available.  Use [NuGet](http://www.nuget.org/) to download official builds.**


---

### FINAL RELEASE NOTICE ###

This will be the last release in it's current form.

The following projects have been moved to github:

  1. /BPlusTree -> [github.com/CSharpTest.Net.Collections](https://github.com/csharptest/CSharpTest.Net.Collections)
  1. /RpcLibrary -> [github.com/CSharpTest.Net.RpcLibrary](https://github.com/csharptest/CSharpTest.Net.RpcLibrary)
  1. /Library/Commands -> [github.com/CSharpTest.Net.Commands](https://github.com/csharptest/CSharpTest.Net.Commands)
  1. /Library/Tools -> [github.com/CSharpTest.Net.Tools](https://github.com/csharptest/CSharpTest.Net.Tools)
  1. /SslTunnel -> [undecided](undecided.md)

**Not sure what will happen to the rest, may reduce the library and move to GitHub, but this repo will close.**


---

### Library History ###
**2.14.126.467**

Additions in this release:

  1. Added CSharpTest.Net.Collections.LurchTable a thread-safe dictionary with the option of linking entries by insert, modify, or access.  (LinkedHashMap for C#)
  1. Added CSharpTest.Net.Data.DbGuid to provide time based sequential GUID generation (COMB Guid).
  1. BPlusTree added INodeStoreWithCount and allowed injection of IObjectKeepAlive implementation.
  1. Added Crc64 for a quick 64-bit CRC using algorithm (CRC-64/XZ)
  1. Added a simple HttpServer class.
  1. Added Services/ServiceAccessRights and Services/SvcControlManager to provide more control over services.
  1. Added Http/MimeMessage to parse raw HTML form where ContentType = "multipart/form-data", including one or more attachments.
  1. Extended the CommandInterpreter to allow exposing console commands over REST services via built-in service.
  1. Added a CallsPerSecondCounter to allow quick estimation of performance metrics in high-traffic code.
  1. Added an experimental TcpServer (currently under test)
  1. Extended the RpcError enumeration to encompass more errors and decorated descriptions.
  1. RpcLibrary now supports RpcServerRegisterIf2 allowing un-authenticated TCP access and control of max TCP request size.
  1. Added the IDL for the RpcLibrary for unmanaged interop

Fixes in this release:

  1. Converted BPlusTree's Storage.Cache to use LurchTable to address memory issues in buggy LRU cache.
  1. Optimization of BPlusTree's TransactedCompoundFile in location of free data blocks.
  1. Fixed the dreaded SEHException in the RpcLibrary, now expect acurate RpcException with correct RpcError details.


**2.12.810.409**

NOTES:  Major version increment primarily due to extensive changes in BPlusTree's capabilities and storage format.  The next release will flag the v1 Options class as Obsolete to ensure people are using the latest version.  All new code should be using 'OptionsV2' when constructing a BPlusTree.

Additions in this release:

  1. Added BPlusTree.OptionsV2 to provide a more simple set of options and uses the v2 file format.
  1. BPlusTree now supports key range enumeration as well as accessing first/last key in the tree.
  1. BPlusTree now supports optimized AddRange and BulkInsert methods.
  1. BPlusTree now supports transaction logging for improved durability and recovery.
  1. BPlusTree now has a completely new file format (use the OptionsV2 class) for improved performance and reliability.
  1. The generator's ResXtoMC complier now supports application manifest files and the inclusion of win32 resource files.
  1. Added the IConcurrentDictionary interface to provide an abstraction on concurrent dictionary implementations.
  1. BPlusTree now supports all relevent members of the .NET ConcurrentDictionary class and implements IConcurrentDictionary.
  1. Updated SynchronizedDictionary to implement IConcurrentDictionary, great for testing code without a BPlusTree backend.
  1. Added BTreeDictionary&lt;TKey,TValue> to support in-memory always sorted key/value dictionary.
  1. Added BTreeList&lt;T> to support an in-memory B+Tree backed sorted list.
  1. Added collection classes to deal with KeyValue comparision, ordering, enumerating, etc.
  1. Added MergeSort (stable sorting) static class for more efficient sorting with custom comparer implementations.
  1. Added an alternative to FragmentedFile called TransactedCompoundFile to support the rewite of the BPlusTree file format.
  1. CmdTool.exe now supports a boolean 'stop' attribute on the FileMatch tag, allowing complete override of files that appear higher in the folder structure.


Fixes in this release:

  1. XmlLightElement CDATA handling was incorrectly decoding html content.
  1. OrdinalList had a bug in the IntersectWith and UnionWith implementations where the inputs were the same length.
  1. BPlusTree enumeration of Keys or Values would load the entire tree into a list, this is now fixed.


Breaking changes in this release:

  1. The following breaking changes were made to BPlusTree to bring it's concurrent interface members inline with .NET 4's ConcurrentDictionary.
    * BPlusTree changed the delegate type provided to TryUpdate to include both Key and Value.
    * BPlusTree changed the method name and delegate type of Add(TKey, delegate) to TryAdd(TKey, delegate).
    * BPlusTree changed the delegate types provided to AddOrUpdate to include both Key and Value.
  1. BPlusTree.Options is still backwards compatible with existing files; however, BPlusTree.OptionsV2 uses a new format.


**v1.11.924.348**

Minor update release:

  1. Addition of Cyrpto.SecureTransfer to provide file transfers via shared public keys.
  1. The Crypto.AESCryptoKey now has ToArray() and FromBytes() like other keys.
  1. HashStream can now aggregate read/write calls to actual storage stream while computing the hash.
  1. The Crypto.Hash class received a new method, Combine(...)
  1. Html.XmlLightElement and related classes are now fully modifiable.
  1. BPlusTree.Options now supports a ReadOnly property to ensure no writes at the file handle level.


**v1.11.426.305**

Additions in this release:

  1. Introduced [CSharpTest.Net.BPlusTree.dll](http://csharptest.net/browse/src/BPlusTree) - a fairly full featured IDictionary implementation backed by a B+Tree on disk.
  1. Collections.[LListNode](http://csharptest.net/browse/src/Library/Collections/LListNode.cs) - a doubly linked list implementation that can support asynchronous iteration.
  1. Collections.[SynchronizedDictionary](http://csharptest.net/browse/src/Library/Collections/SynchronizedDictionary.cs)/[SynchronizedList](http://csharptest.net/browse/src/Library/Collections/SynchronizedList.cs) to support synchronization of a list/dictionary given a locking strategy from the Synchronization namespace.
  1. IO.[ClampedStream](http://csharptest.net/browse/src/Library/IO/ClampedStream.cs) to provide an IO stream aggregation for a subset of the provided stream.
  1. IO.[Crc32](http://csharptest.net/browse/src/Library/IO/Crc32.cs) to provide calculation of a CRC32 value from bytes or strings.
  1. IO.[FileStreamFactory](http://csharptest.net/browse/src/Library/IO/FileStreamFactory.cs) an IFactory producer of streams for a given file.
  1. IO.[FragmentedFile](http://csharptest.net/browse/src/Library/IO/FragmentedFile.cs) an underpinning of the B+Tree implementation that provides sub-allocations within a single file.
  1. IO.[SharedMemoryStream](http://csharptest.net/browse/src/Library/IO/SharedMemoryStream.cs) a block allocating memory stream that can be simultaneously used by multiple threads at the same time.
  1. IO.[StreamCache](http://csharptest.net/browse/src/Library/IO/StreamCache.cs) a pool of open file streams that a thread can open and close without the overhead of actually opening or closing the underlying file streams.
  1. Interfaces.[IFactory](http://csharptest.net/browse/src/Library/Interfaces/IFactory.cs) provides a simple generic factory interface for supplying instances of type T.
  1. Interfaces.[ITransactable](http://csharptest.net/browse/src/Library/Interfaces/ITransactable.cs) provides a simple transaction interface.
  1. IpcChannel.[IpcEventChannel](http://csharptest.net/browse/src/Library/IpcChannel/IpcEventChannel.cs) provides a cross domain/process connectionless communication built on events.  [see this SO post](http://stackoverflow.com/questions/5007247/finding-or-building-an-inter-process-broadcast-communication-channel).
  1. Serialization.[ISerializer](http://csharptest.net/browse/src/Library/Serialization/ISerializer.cs) provides a simple interface for an object that can read and write an instance of type T to and from a stream.
  1. Serialization.[PrimitiveSerializer](http://csharptest.net/browse/src/Library/Serialization/PrimitiveSerializer.cs) provides basic implementation of the ISerializer interface for the primitive types.
  1. Serialization.[VariantNumberSerializer](http://csharptest.net/browse/src/Library/Serialization/VariantNumberSerializer.cs) provides a protobuffer-like encoding for numeric types.
  1. Threading.[WaitAndContinueList](http://csharptest.net/browse/src/Library/Threading/WaitAndContinueList.cs) a work list based on WaitHandles and resulting actions so that multiple activities can be performed on a single thread.
  1. Threading.[WaitAndContinueWorker](http://csharptest.net/browse/src/Library/Threading/WaitAndContinueWorker.cs) a single worker thread that processes a WaitAndContinueList.
  1. [WorkQueue](http://csharptest.net/browse/src/Library/Threading/WorkQueue.cs) and [WorkQueue](http://csharptest.net/browse/src/Library/Threading/WorkQueue.cs) provide simple thread pool processing of tasks that the caller can wait for completion on.
  1. Utils.[ObjectKeepAlive](http://csharptest.net/browse/src/Library/Utils/ObjectKeepAlive.cs) a simple object to track references to other instances to avoid garbage collection.
  1. Utils.[WeakReference](http://csharptest.net/browse/src/Library/Utils/WeakReference.cs) a derivation of WeakReference that is type-safe.
  1. [Synchronization](http://csharptest.net/browse/src/Library/Synchronization) classes are newly rewritten.


Breaking changes in this release:

  1. The [Synchronization namespace](http://csharptest.net/browse/src/Library/Synchronization) has undergone a complete overhaul.  If your currently depending upon it's interfaces or implementation you may want to stay with the version you have until you can determine the impact.  Some simple uses of the previous classes may still work, but this a complete rewrite.  Why?  Simply put the last version was junk.  The added cost of the abstraction layer was more than the lock itself.  I've retooled it to avoid new instances on lock, removed the use of TimeSpan, removed the upgrade locks, and simplified the interfaces.  The end result is a very clean interface that is easy to use and fast.


**April 26th, 2011**

**Fully converted over to mercurial...**

**v1.10.1124.358**

  1. Introduction of the RpcLibrary to provide pure c# interop with the Win32 RPC API.
  1. Added GeneratedCode attributes to nested classes within the resource generator

**v1.10.1102.349**

  1. Added Library.Processes.AssemblyRunner to provide execution of managed EXE files inside an appdomain while still provide redirection of std IO.
  1. Bug fixes in CSharpTest.Net.Generators.exe
  1. Bug fixes and performance issues in CmdTool.exe

**v1.10.1024.336**

  1. Added CSharpTest.Net.Generators.exe to integrate with the CmdTool's VS integration:
    * Provides ResX loose-typed string formatting via simply using "{0}" in a resource string.
    * Provides ResX strong-typed string formatting via resource names like "Name(string x)"
    * Adds exception generation to resources via names like "NameException"
    * Exceptions can be derived from explicit type via comments: " : ArgumentException"
  1. Added Crypto.ModifiedRijndael class to overload the construction of the of BCL's RijndaelManaged transform.  This allows you to specify non-AES-standard key lengths and encryption rounds supported by the Rijndael algorithm.
  1. Added Formatting namespace to contain classes for string-based byte encoding.
    * Base64Stream - Streams data to a text format using base-64 encoding.
    * ByteEncoding - Provides a base abstract class/interface for encoding bytes.
    * HexEncoding - Converts a series of bytes to a string of hexidecimal charcters.
    * HexStream - Streams data to a text format using hexidecimal characters.
    * Safe64Encoding - Replaces AsciiEncoder, a base-64 derived encoding ('-_' instead of '+/=')
    * Safe64Stream - Streams data to a text format using the Safe64Encoding.
  1. Extended the IEncryptDecrypt interface to support string encryption with a specified byte encoding.
  1. Merged the 'Shared source' files with the Library_

**v1.10.913.269**
  1. Added CmdTool.exe - the last Visual Studio code generator you'll ever need :)
    * Code generation made easy, just write a command line tool.
    * No shutting down Visual Studio when you change your code generation tool.
    * Integrates with Visual Studio 2005, 2008, or 2010.
    * Displays console output in Visual Studio's output window.
    * Clean or Build generated content directly from a command-line.
    * Self-registering, simply run: CmdTool.exe register
    * Read [the sample configuration file](http://csharptest-net.googlecode.com/svn/trunk/src/Tools/CmdTool/CmdTool.config) for more information.
  1. Added CSharpTest.Net.Bases.Disposable - a default base class implementing IDisposable with a Disposed event.
  1. Added CSharpTest.Net.Crypto.HashStream - for creating a hash value by simply writing values to a stream.
  1. Added CSharpTest.Net.Delegates.TimeoutAction - for executing a delegate after a fixed period of time.
  1. Added CSharpTest.Net.IO.TempDirectory - same as TempFile but creates/deletes an entire directory.
  1. Added CSharpTest.Net.Processes.ScriptRunner - for executing and capturing the output of various scripts.
  1. Added CSharpTest.Net.Synchronization namespace - Provides common interfaces to reader/writer and exclusive locking.
  1. CSharpTest.Net.Delegates.EventHandlerForControl - Fix for COM hosted controls - TopLevelControl returns null.
  1. CSharpTest.Net.Html.IXmlLightReader - Breaking change - extended Start/End tag with structured argument.
  1. CSharpTest.Net.Html.XmlLightElement - Now has the ability to recreate the input via WriteUnformatted(TextWriter).
  1. CSharpTest.Net.Processes.ProcessRunner - Fixed some issues with IsRunning on STA threads, and fixed Kill().
  1. CSharpTest.Net.Reflection.PropertyType - Now exposes attributes defined on the member reflected.
  1. Build.bat - Default framework is now 3.5, see CSBuild.exe.config to change build to 2.0.
**v1.10.607.213**
  1. Library.Crypto namespace was added with a fairly complete cryptography API (at least what I needed) including:
    * Added Library.Crypto.WhirlpoolManaged a managed implementation of the whirlpool hash function.
    * Added Library.Crypto.Password to wrap up the complexities of using a password for authentication and/or encryption.
    * Loads of other stuff from managing RSA keys to creating verifying salted hashs etc.
  1. Library.IO namespace was added to include several new stream derivations including:
    * Added Library.IO.SegmentedMemoryStream for memory streaming while avoiding LOH allocations.
    * Added Library.IO.TempFile to manage temp files and remove them when disposed.
    * Added Library.IO.ReplaceFile to transact replacing a file.
  1. Library.Html namespace was added to help with manipulation of html and xhtml:
    * Added Library.Html.XhtmlValidation will use the w3c xhtml 1.0 DTDs to validate xhtml files.
    * Added Library.Html.HtmlLightDocument to a provide fast DOM parsing of HTML using regular expressions.
    * Added Library.Html.XmlLightDocument to a provide fast DOM parsing of XML using regular expressions.
**v1.10.420.164**
  1. CSBuild initial release - a command-line compilation utility that drives MSBuild to compile designated project files.
  1. Added Library.Cloning namespace to support deep-object cloning of any object using either memberwize or serializable copy.
  1. Added Library.Collections.ReadOnlyList to provide a read-only collection interface and implementation.
  1. Added Library.Collections.OrdinalList to provide a collection of integers stored as a bit-array that can be operated on as a set (intersect/union/etc).
  1. Added Library.Collections.SetList to provide a generic collection of that can be operated on as a set (intersect/union/etc).
  1. CommandInterpreter can now read SET operations from stream, also added an IgnoreMember attribute.
**v1.9.1004.144**
  1. Added a command-line interpreter and parser under Library.Commands
  1. Added a WinForms cross-threaded event delegate class that prevents deadlocking
  1. Added Library.Processes.ProcessRunner utility class for spawning a process and correctly collecting the Output
  1. Added a few FileUtils to allow searching the environment path and granting full access on a file for a well-known account
  1. Dropped usage of [DebuggerStepThrough](DebuggerStepThrough.md) attribute
  1. Added static methods on ArgumentList for Join, Parse, and Remove
  1. Added an implementation of a SSL tunneling service for securing non-secure TCP/IP communications
**v1.0.723.126**
  1. Changes mostly encompassed the release of the Jira/SVN integration via the IBugTraqProvider

---
