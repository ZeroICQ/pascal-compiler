type 
    myint = type integer;
    yourint = type myint;
    
    mydouble = type double;
    yourdouble = type double;
    
    mychar = type char;
    yourchar = type mychar;

var
    i0 : myint;
    i1 : yourint;
    i2 : integer;
    
    f0 : mydouble;
    f1 : yourdouble;
    f2 : double;
    
    c0 : mychar;
    c1 : yourchar;
    c2 : char;
    
begin
    i2 := i1 + i0;
    i1 := i0 + i2;
    i0 := i1 + i2;
    
    f2 := f1 + f0;
    f1 := f0 + f2;
    f0 := f1 + f2;
    
    c2 := c0;
    c2 := c1;
    c2 := c2;
    
    c1 := c0;
    c1 := c1;
    c1 := c2;
    
    c0 := c0;
    c0 := c1;
    c0 := char(c2);
end.