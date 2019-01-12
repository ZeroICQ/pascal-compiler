type
    myint = Integer;
    otherInt = type myint;


procedure mem(a: double; b: Integer; c: myint; d: otherInt);
begin
    a := b  + c + d;
end;


var 
    k,l,m,n : integer;
begin
    mem(1, 2, 3, 'c');
end.