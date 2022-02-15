echo Hello

@C:
@cd "C:\Users\stefa\Documents\A A C_Compiler_CSharp_10\Calculator_CS_10"
@"C:\gppg-distro-1_5_2\binaries\Gppg" /gplex Parser.gppg > Parser.cs
@"C:\gppg-distro-1_5_2\binaries\Gplex" Scanner.gplex
@rem @C:\YACC -d Parser.bison

@rem cd C:\Users\Stefa\Documents\Calculator_Bison_Flex\Calculator
@rem "C:\Program Files (x86)\GnuWin32\bin\bison" Parser.bison
@rem "C:\Program Files (x86)\GnuWin32\bin\flex" Scanner.flex

@rem move y.tab.c Parser.cpp
@rem move y.tab.h Parser.h
@rem move lex.yy.c Scanner.cpp

@pause