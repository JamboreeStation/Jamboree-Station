// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Jamboree.Ntsl;

public abstract record NtslExpr(int Line);

public sealed record NtslNumberExpr(int Line, double Value) : NtslExpr(Line);

public sealed record NtslStringExpr(int Line, string Value) : NtslExpr(Line);

public sealed record NtslNameExpr(int Line, string Name) : NtslExpr(Line);

public sealed record NtslListExpr(int Line, List<NtslListEntry> Entries) : NtslExpr(Line);

public sealed record NtslUnaryExpr(int Line, NtslTokenType Op, NtslExpr Operand) : NtslExpr(Line);

public sealed record NtslBinaryExpr(int Line, NtslTokenType Op, NtslExpr Left, NtslExpr Right) : NtslExpr(Line);

public sealed record NtslAssignExpr(int Line, NtslTokenType Op, NtslExpr Target, NtslExpr Value) : NtslExpr(Line);

public sealed record NtslCallExpr(int Line, NtslExpr Callee, List<NtslExpr> Args) : NtslExpr(Line);

public sealed record NtslMemberExpr(int Line, NtslExpr Target, string Name) : NtslExpr(Line);

public sealed record NtslIndexExpr(int Line, NtslExpr Target, NtslExpr Index) : NtslExpr(Line);

public readonly record struct NtslListEntry(NtslExpr? Key, NtslExpr Value);

public abstract record NtslStmt(int Line);

public sealed record NtslExprStmt(int Line, NtslExpr Expr) : NtslStmt(Line);

public sealed record NtslIfStmt(int Line, List<NtslIfBranch> Branches, List<NtslStmt>? Else) : NtslStmt(Line);

public sealed record NtslWhileStmt(int Line, NtslExpr Condition, List<NtslStmt> Body) : NtslStmt(Line);

public sealed record NtslForStmt(int Line, NtslExpr? Init, NtslExpr? Condition, NtslExpr? Step, List<NtslStmt> Body) : NtslStmt(Line);

public sealed record NtslReturnStmt(int Line, NtslExpr? Value) : NtslStmt(Line);

public sealed record NtslBreakStmt(int Line) : NtslStmt(Line);

public sealed record NtslContinueStmt(int Line) : NtslStmt(Line);

public sealed record NtslDefStmt(int Line, string Name, List<string> Params, List<NtslStmt> Body) : NtslStmt(Line);

public readonly record struct NtslIfBranch(NtslExpr Condition, List<NtslStmt> Body);