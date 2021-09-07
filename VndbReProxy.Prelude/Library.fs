[<AutoOpen>]
module VndbReProxy

let inline (^) a b = a b

[<RequiresExplicitTypeArguments>]
let inline ignore<'T> (a: 'T) = ignore a

type Undefined = private | Undefined

let inline undefined<'T> : 'T = raise ^ System.NotImplementedException()
