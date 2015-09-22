module FindDuplicates

open System
open System.IO;
open System.Security.Cryptography

[<EntryPoint>]
let main argv= 

    try
        if argv.Length <> 2 then
            failwith "Expected arguments <File Size (bytes)> and <File Path>"

        let fileSize, dirPath = Int64.Parse(argv.[0]), argv.[1].TrimEnd  [|'\\'|]
    
        if Directory.Exists(dirPath) <> true then 
            failwith (sprintf "The directory \"%s\" does not exist" dirPath)

        let getFiles path =
            Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)

        let getFileHash filePath =
            using (File.OpenRead filePath) (SHA256Managed.Create()).ComputeHash
        
        let getDuplicateFiles dirPath = 
            getFiles dirPath
            |> Seq.map (fun file -> (file, (new FileInfo(file)).Length) ) 
            |> Seq.filter (fun (_, length) -> length >= fileSize)
            |> Seq.groupBy snd
            |> Seq.collect (fun (_, filesWithSameLength) ->
                if (Seq.length filesWithSameLength) = 1 then Seq.empty
                else
                    filesWithSameLength
                    |> Seq.map (fun (filePath, length) -> (filePath, getFileHash(filePath), length ))
                    |> Seq.groupBy (fun (_,hash, length) -> length, hash )  
                    |> Seq.filter (fun (_, filesWithSameLengthHashed) -> Seq.length filesWithSameLengthHashed > 1))
    
        let duplicates = getDuplicateFiles dirPath  
    
        let printOutDuplicateFiles ( duplicates : seq<(int64*byte[])*seq<(string*byte[]*int64)>> ) =
            if Seq.length duplicates > 0 then
                printfn "Dublicates were located under the folder %s\n" dirPath
                duplicates 
                |> Seq.iter (fun ((length, hash), values) -> 
                    printfn "File Size: %d bytes, Hash: %A" length (System.BitConverter.ToString(hash).Replace("-", ""))
                    values
                    |> Seq.iter (fun (filePath, _,_) -> printfn "\tFile path: %s" filePath ) )
            else
                printfn "No duplicate files were found under the folder %s" dirPath
        
        printOutDuplicateFiles duplicates   

    with
    | _ as ex ->
        printfn "An error occured\n. %s" ex.Message 

    0 // return an integer exit code
