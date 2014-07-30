(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

(**
NullKill
===================

Documentation

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      NullKill can be <a href="https://nuget.org/packages/NullKill">installed from NuGet</a>:
      <pre>PM> Install-Package NullKill</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

Example
-------

This example usage of NullKill:

*)
#r "NullKill.dll"
open NullKill

let myThing = System.DateTime.Now

match Check myThing with
| Some thing ->
    "myThing is not null, and neither are any of it's fields or properties."
| None ->
    "This will never get called."

let nastyThing = null

if HasNoNulls nastyThing then
    "This would be nice, but..."
else
    "nastyThing is null, so you'll run this path."

type NoGoodClass () =
    [<DefaultValue>] val mutable Field : System.DateTime
    member val Prop1 = null with get, set

match Check <| NoGoodClass () with
| Some ngc ->
    "Won't get here, as there are nasty nulls lurking."
| None ->
    "We'll get this instead."

match Check <| NoGoodClass(Field = System.DateTime.Now, Prop1 = "Hello world") with
| Some ngc ->
    "All set up this time."
| None ->
    "We only have to worry about nulls on this route."

(**

Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding new public API, please also 
consider adding [samples][content] that can be turned into a documentation. You might
also want to read [library design notes][readme] to understand how it works.

The library is available under Public Domain license, which allows modification and 
redistribution for both commercial and non-commercial purposes. For more information see the 
[License file][license] in the GitHub repository. 

  [content]: https://github.com/mavnn/NullKill/tree/master/docs/content
  [gh]: https://github.com/mavnn/NullKill
  [issues]: https://github.com/mavnn/NullKill/issues
  [readme]: https://github.com/mavnn/NullKill/blob/master/README.md
  [license]: https://github.com/mavnn/NullKill/blob/master/LICENSE.txt
*)
