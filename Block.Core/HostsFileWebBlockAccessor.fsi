namespace Block.Core
module HostsFileWebBlockAccessor =
    open Contracts

    val blockSite : FileLocator -> BlockedSite -> unit

    val unblockSite : FileLocator -> BlockedSite -> unit

    val listBlockedSites : FileLocator -> unit -> BlockedSite seq

    //val getBlockedDomains : unit 