module ConstraintProto

type Constraint<'a> = 
    | MaxLength of int
    | MinLength of int
    | Regex of string // regex expression
    | Max of 'a // should be any type of number
    | Min of 'a
    | Choice of 'a list
    | NonEmpty
    | And of Constraint<'a> list
    | Or of Constraint<'a> list
    | Custom of string * ('a -> bool) // the tag needn't be a string

    // do I need to segment some of these by types they can apply to?

module Constraint = 
    let rec catamorph maxLengthf minLengthf maxf minf choicef nonemptyf andf orf customf inst : 'r =
        let recurse = catamorph maxLengthf minLengthf maxf minf choicef nonemptyf andf orf customf
        match inst with
        | MaxLength len -> maxLengthf len
        | MinLength len -> minLengthf len
        //| Regex expr -> regexf expr
        | Max limit -> maxf limit
        | Min limit -> minf limit
        | And children -> children |> List.map recurse |> andf 
        | Or children -> children |> List.map recurse |> orf 
        //....

    let (&&&) left right = And [left; right]
    let and' left right = And [left; right]
    let andAll list = And list
    let or' left right = Or [left; right]
    let orAny list = Or list
    let matchRegex expr = Regex expr

    let (|IsComparable|) (obj : obj) = 
        match obj with
        | :? System.IComparable as comparable -> Some(comparable)
        | _ ->  None

    module DefaultValidations = 
        open FSharpx.Result


        let validateMax max value = 
            match value with
            | v when v < max -> Ok v
            | _ -> Error [$"{value} is not less than {max}"]
            //match value with
            //| :? System.IComparable as valid -> 
            //    match valid with 
            //    | Some v when v < max -> Ok v
            //    | _ -> Error "" // Should have a result type that maintains info and can be formatted later | InvalidConstraint | OverMax (val, max) | UnderMin..., that also makes explain easy
            //| _ -> Error "Invalid constraint for the given type" // can't do anything with this
        

        let validateAnd childResults = 
            let combine left right =
                match (left, right) with 
                | Ok _, Ok _ -> left
                | Ok _, Error err -> Error err
                | Error err, Ok _ -> Error err
                | Error errLeft, Error errRight -> Error (List.concat [errLeft; errRight])
            childResults |> List.reduce combine

    //let validate value = 
    //    let avoiding = id

    //    // I need to get my head on straight about what i'm folding here. Am I creating a function? can I fold from a result?
    //    // Is this Constraint -> (value -> Result) or is it Constraint -> Result
    //    // right now the fold doesn't have access to some initial value. Thus it isn't fold but reduce. I have no choice but to reduce to a function
    //    // unless I refactor to be a true fold... right? 

    //    // what I actually need is apply. If it weren't recursive I could just apply a list, but no.. I have to figure out recursive application
    //    let appliedFold = catamorph 
    //                        <| avoiding
    //                        <| avoiding
    //                        <| (>>) (DefaultValidations.validateMax)
    //                        <| avoiding
    //                        <| avoiding
    //                        <| avoiding
    //                        <| ((>>) DefaultValidations.validateAnd)
    //                        <| ((>>) DefaultValidations.validateAnd)
    //                        <| avoiding
    //                        //avoiding avoiding DefaultValidations.validateMax avoiding avoiding avoiding (DefaultValidations.validateAnd) avoiding avoiding
    //    appliedFold id


    // how do I match these against properties/values
    // - Min/max require comparable
    // - Length requires ... IEnumarable?
    // - regex only works on strings
    // - choice requires equality
    // how do I handle cases that don't apply to a type of value?
    // - right now i'm just requiring the constraints, but I would like to remove it by checking for constraint compliance in the validation method