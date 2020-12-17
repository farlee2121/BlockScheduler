module StreamExtensions

open System.IO

module Stream = 
    let Rewind (stream:Stream) = stream.Position <- int64 0; stream

    
    let WriteAllLines (stream:Stream) (lines:seq<string>) =
        // conctrasting the interative approach to the recursive read approach
        let writer = (new StreamWriter(Rewind stream))
        lines |> Seq.iter (fun line -> writer.WriteLine(line))
        writer.Flush();
        Rewind stream

    let ReadAllLines (stream:Stream) = 
        let rec ReadAllLinesRec (streamReader:StreamReader) = 
            // I think I could also do this with a list/sequence comprehension?
            match streamReader.ReadLine() with
            | null -> []
            | value -> value :: ReadAllLinesRec streamReader

        ReadAllLinesRec (new StreamReader(Rewind stream))

    let WriteAllText (stream:Stream) (text:string) = 
        let writer = new System.IO.StreamWriter(stream)
        writer.Write(text)
        writer.Flush()
        Rewind stream