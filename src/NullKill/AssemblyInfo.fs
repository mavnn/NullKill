namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("NullKill")>]
[<assembly: AssemblyProductAttribute("NullKill")>]
[<assembly: AssemblyDescriptionAttribute("Check some common sources of nulls")>]
[<assembly: AssemblyVersionAttribute("0.10.0.0")>]
[<assembly: AssemblyFileVersionAttribute("0.10.0.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.10.0.0"
