module SeqExtensions

module Seq =
    let takeUntil (predicate:'T->bool) (sequence:seq<'T>) = 
        let breakIndex = 
            sequence 
            |> Seq.tryFindIndex predicate
            |> Option.defaultValue (Seq.length sequence)
        Seq.take breakIndex sequence
    let takeUntilInclusive (predicate:'T->bool) (sequence:seq<'T>) = 
        let breakIndex = 
            sequence 
            |> Seq.tryFindIndex predicate
            |> Option.defaultValue (Seq.length sequence)
        Seq.take (breakIndex+1) sequence
