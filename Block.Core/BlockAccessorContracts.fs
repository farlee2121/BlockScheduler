namespace Block.Core
module Contracts =
    type public BlockedSite = { Url:string; }
    type FileLocator = Stream of System.IO.Stream | Path of string | Uri of System.Uri 
