module StreamExtensions

open System.IO

module Stream = 
    let Rewind (stream:Stream) = stream.Position <- int64 0; stream

    let rec private ReadAllLinesRec (streamReader:StreamReader) = 
        match streamReader.ReadLine() with
        | null -> []
        | value -> value :: ReadAllLinesRec streamReader


    let rec WriteAllLines (stream:Stream) (lines:seq<string>) =
        // conctrasting the interative approach to the recursive read approach
        let writer = (new StreamWriter(Rewind stream))
        lines |> Seq.iter (fun line -> writer.WriteLine(line))
        writer.Flush();
        Rewind stream

    let ReadAllLines (stream:Stream) = ReadAllLinesRec (new StreamReader(Rewind stream))

    let WriteAllText (stream:Stream) (text:string) = 
        let writer = new System.IO.StreamWriter(stream)
        writer.Write(text)
        writer.Flush()
        Rewind stream