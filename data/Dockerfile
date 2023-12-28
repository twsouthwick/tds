FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

COPY mssql.key /etc/ssl/private/
COPY mssql.pem /etc/ssl/certs/

RUN chmod 600 /etc/ssl/private/mssql.key /etc/ssl/certs/mssql.pem
RUN chown mssql /etc/ssl/private/mssql.key /etc/ssl/certs/mssql.pem
RUN cp /etc/ssl/certs/mssql.pem /usr/share/ca-certificates/mssql.crt
RUN dpkg-reconfigure ca-certificates

RUN /opt/mssql/bin/mssql-conf set network.tlscert /etc/ssl/certs/mssql.pem
RUN /opt/mssql/bin/mssql-conf set network.tlskey /etc/ssl/private/mssql.key
RUN /opt/mssql/bin/mssql-conf set network.tlsprotocols 1.2
RUN /opt/mssql/bin/mssql-conf set network.forceencryption 1

#USER mssql
CMD [ "/opt/mssql/bin/sqlservr" ]