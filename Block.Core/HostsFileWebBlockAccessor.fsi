namespace Block.Core
module HostsFileWebBlockAccessor =
    open Contracts

    type FileLocator = Stream of System.IO.Stream | Path of string | Uri of System.Uri 

    val blockSite : FileLocator -> BlockedSite -> unit

    val unblockSite : FileLocator -> BlockedSite -> unit

    val listBlockedSites : FileLocator -> unit -> BlockedSite seq

    //val getBlockedDomains : unit 