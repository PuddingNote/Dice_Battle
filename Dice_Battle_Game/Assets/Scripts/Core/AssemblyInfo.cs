using System.Runtime.CompilerServices;

// EditMode 테스트에서 결정 상황(보드/손패)을 직접 구성할 수 있도록
// GameState 의 internal setter 등에 접근을 허용한다.
[assembly: InternalsVisibleTo("DiceBattle.Tests.EditMode")]
