var
    a: integer = 10;
    b: integer = 10;
begin
    if (a <> b) then
        writeln( 'not ok' )
    else
        writeln( 'ok' );

    if (a = b) then
        writeln( 'ok' );

    if (a > b) then
        writeln('not ok')
    else if ( a = 10) then
        writeln('ok')
    else 
        writeln('not ok x2');
end.    