module NullKill.Tests

open NullKill
open NUnit.Framework

type Testable() =
    [<DefaultValue>] val mutable Field1 : System.DateTime
    member val Prop1 = null with get, set
    member val Prop2 = null with get, set

[<Test>]
let ``null objects should fail`` () =
    HasNoNulls null
    |> Assert.IsFalse

[<Test>]
let ``NullKill should detect null properties`` () =
    let t = Testable()
    t.Field1 <- System.DateTime.Now
    let r = HasNoNulls (t)
    Assert.IsFalse(r, "Null property values not detected")

[<Test>]
let ``NullKill should detect null fields`` () =
    let t = Testable()
    t.Prop1 <- System.DateTime.Now :> obj
    t.Prop2 <- System.DateTime.Now :> obj
    let r = HasNoNulls (Testable())
    Assert.IsFalse(r, "Null field values not detected")

[<Test>]
let ``NullKill should pass objects with no nulls`` () =
    let t = Testable()
    t.Prop1 <- System.DateTime.Now :> obj
    t.Prop2 <- System.DateTime.Now :> obj
    t.Field1 <- System.DateTime.Now
    let r = HasNoNulls t
    Assert.IsTrue(r, "No null fields or properties - should pass")