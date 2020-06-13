namespace Block.Core

module HostsFileWebBlockAccessor =


    type public BlockedWebAddress = { Url:string; }
    val blockDomain : (BlockedWebAddress seq) -> (string) -> (BlockedWebAddress seq)

    val unblockDomain : (BlockedWebAddress seq) -> string -> (BlockedWebAddress seq)

    //val getBlockedDomains : unit 