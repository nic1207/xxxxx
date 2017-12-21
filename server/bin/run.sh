#!/bin/sh
#running the process cwmd and then starting loginapp
#timerd dbmgr base cell 
echo "[running process cwmd......]"
#who=`whoami`
#echo $who
#if [ "$who" = "root" ]
#then
#    echo "can't start server via root"
#    exit
#fi
path=`pwd`
#echo "--------- sync_db -----------"
${path}/sync_db ./cfg.ini
#echo "--------- cwmd -----------"
${path}/cwmd ./cfg.ini &
#echo "--------- loginapp -----------"
${path}/loginapp ./cfg.ini 1 ./log/loginlog_1 &
#echo "--------- dbmgr -----------"
${path}/dbmgr ./cfg.ini 3 ./log/dblog_3 &
#echo "--------- timerd -----------"
${path}/timerd ./cfg.ini 4 ./log/timerd_4 &
#echo "--------- logapp -----------"
${path}/logapp ./cfg.ini 5 ./log/logapplog_5 &
#echo "--------- baseapp -----------"
${path}/baseapp ./cfg.ini 6 ./log/baselog_6 &
#echo "--------- cellapp -----------"
${path}/cellapp ./cfg.ini 7 ./log/celllog_7 &



