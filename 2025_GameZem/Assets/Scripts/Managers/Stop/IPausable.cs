// 어떤 컴포넌트든 이 인터페이스를 구현하면 전역 Pause/Resume 신호를 받을 수 있습니다.
public interface IPausable
{
    void OnPaused();
    void OnResumed();
}
