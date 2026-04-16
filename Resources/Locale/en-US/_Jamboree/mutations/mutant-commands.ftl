command-lsmutants-description = List mutants.
command-lsmutants-help = No arguments.

command-addmutation-description = Initialize an entity as Mutant with a given MutationPrototype
command-addmutation-help = Argument 1 must be an EntityUid, and argument 2 must be a string matching the PrototypeId of a Mutation.
addmutation-args-one-error = Argument 1 must be an EntityUid
addmutation-args-two-error = Argument 2 must match the PrototypeId of a Mutation

command-addrandommutation-description = Initialize an entity as a Mutant with a random Mutation that is available for that entity to roll.
command-addrandommutation-help = Argument 1 must be an EntityUid.
addrandommutation-args-one-error = Argument 1 must be an EntityUid

command-removemutation-description = Remove a Mutation from an entity.
removemutation-args-one-error = Argument 1 must be an EntityUid
removemutation-args-two-error = Argument 2 must match the PrototypeId of a Mutation.
removemutation-not-mutant-error = The target entity is a Mutant.
removemutation-not-contains-error = The target entity does not have this Mutation.

command-removeallmutations-description = Remove all Mutations from an entity.
command-removeallmutations-help = Argument 1 must be an EntityUid.
removeallmutations-args-one-error = Argument 1 must be an EntityUid.
removeallmutations-not-mutant-error = The target entity is not a Mutant.