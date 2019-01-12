type
    myint = Integer;
    yourint = myint;
    
    mydouble = double;
    yourdouble = mydouble;
    theirsdouble = yourdouble;

var
    a : MyInt;
    b : yourint;
    c : Integer;
    
    f1 : double; f2 : mydouble; f3 : yourdouble; f4 : theirsdouble;
 
begin
    c := a + b;
    
    f4 := f3 / f2 * f1 + a;
end.