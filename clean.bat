@echo off
setlocal enabledelayedexpansion

rd /s /q smTablebases\smTablebases\bin
rd /s /q smTablebases\smTablebases\obj
rd /s /q smTablebases\LC\bin
rd /s /q smTablebases\LC\obj
rd /s /q smTablebases\TBacc\bin
rd /s /q smTablebases\TBacc\obj

echo Clean done!
