module ConstraintProto

open FsCheck
open Fare

type Constraint<'a> = 
    | MaxLength of int
    | MinLength of int
    | Regex of string // regex expression
    | Max of 'a // should be any type of number
    | Min of 'a
    | Choice of 'a list
    | And of Constraint<'a> list
    | Or of Constraint<'a> list
    | Custom of string * ('a -> bool) // the tag needn't be a string

    // do I need to segment some of these by types they can apply to?

module Constraint = 
    let rec catamorph maxLengthf minLengthf maxf minf regexf choicef andf orf customf inst : 'r =
        let recurse = catamorph maxLengthf minLengthf maxf minf regexf choicef andf orf customf
        match inst with
        | MaxLength len -> maxLengthf len
        | MinLength len -> minLengthf len
        | Regex expr -> regexf expr
        | Choice list -> choicef list
        | Max limit -> maxf limit
        | Min limit -> minf limit
        | And children -> children |> List.map recurse |> andf 
        | Or children -> children |> List.map recurse |> orf
        | Custom (name, predicate)  -> customf (name, predicate)
        //....

    let (&&&) left right = And [left; right]
    let and' left right = And [left; right]
    let andAll list = And list
    let or' left right = Or [left; right]
    let orAny list = Or list
    let matchRegex expr = Regex expr
    let max limit = Max limit
    let min limit = Min limit
    let maxLength maxLen = MaxLength maxLen
    let minLength minLen = MinLength minLen
    let custom tag pred = Custom (tag, pred)
    let oneOf set = Choice set


    let (|IsComparable|) (obj : obj) = 
        match obj with
        | :? System.IComparable as comparable -> Some(comparable)
        | _ ->  None

    module DefaultValidations = 

        let validateMaxLength<'a> (value: 'a) maxLen =
            let enumToList (enum:System.Collections.IEnumerable) = [for item in enum do yield item]
            match value :> System.Object with
            | :? System.Collections.IEnumerable as enum -> 
                match enum |> enumToList |> List.length with
                | len when len < maxLen -> Ok value
                | _ -> Error [$"Max expected length is {maxLen}"]
            | _ -> Error [$"Invalid Constraint: MinLength cannot be applied to {typeof<'a>.Name}"]

        let validateMinLength<'a> (value: 'a) minLen =
            let enumToList (enum:System.Collections.IEnumerable) = [for item in enum do yield item]
            match value :> System.Object with
            | :? System.Collections.IEnumerable as enum -> 
                match enum |> enumToList |> List.length with
                | len when minLen < len -> Ok value
                | _ -> Error [$"Min expected length is {minLen}"]
            | _ -> Error [$"Invalid Constraint: MinLength cannot be applied to {typeof<'a>.Name}"]

        let validateMax value max = 
            match value with
            | v when v <= max -> Ok v
            | _ -> Error [$"{value} is greater than the max {max}"]

        let validateMin value min = 
            match value with
            | v when min <= v -> Ok v
            | _ -> Error [$"{value} is less than the min {min}"]
            //match value with
            //| :? System.IComparable as valid -> 
            //    match valid with 
            //    | Some v when v < max -> Ok v
            //    | _ -> Error "" // Should have a result type that maintains info and can be formatted later | InvalidConstraint | OverMax (val, max) | UnderMin..., that also makes explain easy
            //| _ -> Error "Invalid constraint for the given type" // can't do anything with this

        let validateRegex value regex = 
            let regexTest = System.Text.RegularExpressions.Regex(regex);
            try
                match value :> System.Object with
                | :? System.String as str ->
                    match regexTest.IsMatch(str) with
                    | true -> Ok value
                    | false -> Error [$"{value} didn't match expression {regex}"]
                | _ -> Error ["Invalid "]
            with
            | e -> Error [$"Cast to Object failed with exception: {e.Message}"]

        let validateChoice value options =
            if options |> List.contains value 
            then Ok value
            else Error [$"{value} not in allowed values %A{options}"]
        

        let validateAnd childResults = 
            let combine left right =
                match (left, right) with 
                | Ok _, Ok _ -> left
                | Ok _, Error err -> Error err
                | Error err, Ok _ -> Error err
                | Error errLeft, Error errRight -> Error (List.concat [errLeft; errRight])
            childResults |> List.reduce combine

        let validateOr childResults = 
            let combine left right =
                match (left, right) with 
                | Ok _, Ok _ -> left
                | Ok ok, Error _ -> Ok ok
                | Error err, Ok ok -> Ok ok
                | Error errLeft, Error errRight -> Error (List.concat [errLeft; errRight])
            childResults |> List.reduce combine

        let validatePredicate value (name, predicate) =
            match predicate value with
            | true -> Ok value
            | false -> Error [$"Failed custom predicate: {name}"]

    let validate constraint' value = 
        let avoiding a = Ok value 

        
        // NOTE: Implemented with values baked in to the validators because it was easier to reason about applicative evaluation
        //       and I wanted it to return all possible errors
        let reduceToResultForValue = catamorph 
                                    <| DefaultValidations.validateMaxLength value
                                    <| DefaultValidations.validateMinLength value
                                    <| (DefaultValidations.validateMax value)
                                    <| DefaultValidations.validateMin value
                                    <| DefaultValidations.validateRegex value
                                    <| DefaultValidations.validateChoice value
                                    <| DefaultValidations.validateAnd
                                    <| DefaultValidations.validateOr
                                    <| DefaultValidations.validatePredicate value
                                    //avoiding avoiding DefaultValidations.validateMax avoiding avoiding avoiding (DefaultValidations.validateAnd) avoiding avoiding
        reduceToResultForValue constraint'

    //TODO: Explain should allow them to pass a config with overrides. At least a map between custom tests and message formatters 


    //module DefaultGenerators =
    //    let minGen = () 

    //    let regexGen pattern = 
    //        Gen.sized (fun size ->
    //            let xeger = Xeger pattern
    //            let count = if size < 1 then 1 else size
    //            [ for i in 1..count -> xeger.Generate() ]
    //            |> Gen.elements
    //            |> Gen.resize count)


    //let toGen constraint' =
    //    let reduceToGen = catamorph 
    //                                <| DefaultValidations.validateMaxLength value
    //                                <| DefaultValidations.validateMinLength value
    //                                <| (DefaultValidations.validateMax value)
    //                                <| DefaultValidations.validateMin value
    //                                <| DefaultGenerators.regexGen
    //                                <| DefaultValidations.validateChoice value
    //                                <| DefaultValidations.validateAnd
    //                                <| DefaultValidations.validateOr
    //                                <| DefaultValidations.validatePredicate value
    //    reduceToGen constraint'

    // Could possibly flatten into an array of AND groups? then I can split them between source and filter types
    // compatible sources can be anded together 
    // finding range overlap, finding choice options in a range and regexes together, eliminate choices not compatible with regex
    // The interpretation of length depends on the type... I think we handle it by string and list cases. Those are the main groups
    // Then filter on custom,
    // If incompatible constraints are specified (max int + minLength) then return an error
    // Another issue is that range appears to only apply for ints. I'd need a separate string range concept, or more likely just error for min/max with strings
    // I still want to handle the DU, Guid, and other valid comparables though...


// How should I handle errors? 
// the value will be available up front and isn't necessary in the return type
// I could just return the constraint, but what about nesting? I think i'd need to return a tree?
// how do I apply to aggregated types? I either need to map properties manually
// (and probably add some helper for naming properties in the errors) or use reflection. Maybe a separate union for representing a tree of (proptery * constraint)

//type ComplexTypeConstraint = 
//| PropertyConstraint of System.Reflection.PropertyInfo * Constraint
//| List of Constraint
//| Complex of ComplexTypeConstraint list