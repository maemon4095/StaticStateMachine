namespace StaticStateMachine.Generator;

internal static class Name
{
    public static string Namespace => nameof(StaticStateMachine);
    public static string StateMachineAttribute => "StaticStateMachineAttribute";
    public static string AssociationAttribute => "AssociationAttribute";
    public static string StateMachineAttributeFull => $"{Namespace}.{StateMachineAttribute}";
    public static string AssociationAttributeFull => $"{Namespace}.{AssociationAttribute}";
    public static string MachineState => "MachineState";
    public static string MachineStateFull => $"{Namespace}.{MachineState}";
    public static string ResettableStateMachine => "IResettableStateMachine";
    public static string ResettableStateMachineFull => $"{Namespace}.{ResettableStateMachine}";
    public static string StateMachineCategory => nameof(StaticStateMachine.StateMachineCategory);
    public static string StateMachineCategoryFull => $"{Namespace}.{StateMachineCategory}";
}