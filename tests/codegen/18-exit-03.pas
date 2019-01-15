function foo1(): integer;
begin
    writeln( 'call foo1' );
    exit(1);
end;

function foo0(): integer;
begin
    writeln( 'call foo0' );
    exit(0);
end;

function foo222(): integer;
begin
    exit(222);
end;

begin
    writeln(foo222());
    if foo1() <> 0 then
        writeln( 'ok' )
    else
        writeln( 'error' );

    if not (foo0()<>0) then
        writeln( 'ok' )
    else
        writeln( 'error' );
end.