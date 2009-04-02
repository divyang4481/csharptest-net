<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt">
  <!--
  Copyright 2009 by Roger Knapp, Licensed under the Apache License, Version 2.0

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
  -->
  <xsl:output method="html" encoding="utf-8" indent="no" omit-xml-declaration="yes" version="1.11" />

  <xsl:template match="/">
    <html>
      <head>
        <xsl:comment xml:space="preserve">
          Copyright 2009 by Roger Knapp, Licensed under the Apache License, Version 2.0

          Licensed under the Apache License, Version 2.0 (the "License");
          you may not use this file except in compliance with the License.
          You may obtain a copy of the License at

          http://www.apache.org/licenses/LICENSE-2.0

          Unless required by applicable law or agreed to in writing, software
          distributed under the License is distributed on an "AS IS" BASIS,
          WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
          See the License for the specific language governing permissions and
          limitations under the License.
        </xsl:comment>
        <title>Coverage Report : <xsl:value-of select="/coverageReport/@reportTitle"/></title>
        <style>
          body, td { font-family: tahoma, arial; font-size: 8pt; color: black; }
          a, a:visited, a:hover, a:link, a:active { text-decoration: none; color: black; }
          a:hover { text-decoration: underline; }

          .header { border: solid 1px black; background-color: #F0F0F0; width: 700px; height: 124px; padding-left: 2px; padding-right: 2px; }
          .topheader { height: 124px; }
          .title { font-weight: bold; font-size: 13pt; border-bottom: inset 2px white; height: 24px; }

          .header table { width: 340px; height: 90px; }
          .header table tr { height: 18px; }
          .header table.right { float:right; border-left: outset 2px white; }
          .header table.left { }
          .header table tr td.c0 { font-weight: bold; padding: 2px; padding-left: 10px; width: 170px; }

          .bargraph { width: 200px; height: 12px; font-weight: bold; }
          .bargood, .barbad, .barfull { width: 150px; height: 12px; border: solid 1px black; float: right; }
          .bargood  { background-color: yellow; }
          .barbad   { background-color: #f00000; }
          .barpass, .barfull { background-color: #00e000; }
          .barpass  { width: 1px; height: 12px; border-right: solid 1px black; }

          .links { margin-top: 10px; }

          .content { width: 700px; border-left: solid 1px #e0e0e0; margin-top: 10px; }
          .content .header { text-transform: capitalize; height: 16px; font-weight: bold; font-size: 10pt; }
          .content .list { height: 16px; padding: 2px; padding-left: 4px; border: solid 1px #e0e0e0; border-top: none; border-left: none; }
          .content .list:hover { background-color: #FFFFE1; }
          .content .name { float: left; width: 450px; overflow: hidden; }
          .content .bargraph { float: right; }

          .child { padding-left: 20px; }
          .bold { font-weight: bold; }
          .type { display: inline; text-transform: capitalize; }
        </style>
      </head>
      <body>
        <xsl:apply-templates select="//coverageReport" mode="header" />
        <xsl:apply-templates select="//coverageReport/modules" mode="content" />
        <xsl:apply-templates select="//coverageReport/namespaces" mode="content" />
      </body>
    </html>
  </xsl:template>

  <!-- header -->
  <xsl:template match="//coverageReport" mode="header">
    <div class="header topheader" cellpadding="2" cellspacing="0">
      <div class="title">
        <xsl:value-of select="project/@name"/>
      </div>
      <table class="right" cellspacing="0" cellpadding="0">
        <tr><td class="c0">Classes:</td><td class="c1"><xsl:value-of select="project/@classes" /></td></tr>
        <tr><td class="c0">Source Files:</td><td class="c1"><xsl:value-of select="project/@files" /></td></tr>
        <tr><td class="c0">Source Lines:</td><td class="c1"><xsl:value-of select="project/@nonCommentLines" /></td></tr>
        <tr><td class="c0">Statement Coverage:</td><td class="c1">
            <xsl:call-template name="bargraph">
                 <xsl:with-param name="percent" select="project/@coverage" />
                 <xsl:with-param name="total" select="project/@sequencePoints" />
                 <xsl:with-param name="remain" select="project/@unvisitedPoints" />
            </xsl:call-template>
        </td></tr>
        <tr><td class="c0">Functional Coverage:</td><td class="c1">
          <xsl:call-template name="bargraph">
            <xsl:with-param name="percent" select="project/@functionCoverage" />
            <xsl:with-param name="total" select="project/@members" />
            <xsl:with-param name="remain" select="project/@unvisitedFunctions" />
          </xsl:call-template>
          </td></tr>
      </table>
      <table class="left" cellspacing="0" cellpadding="0">
        <tr><td class="c0">Coverage Date:</td><td class="c1"><xsl:value-of select="@date" /></td></tr>
        <tr><td class="c0">Coverage Time:</td><td class="c1"><xsl:value-of select="@time" /></td></tr>
        <tr><td class="c0">Version:</td><td class="c1"><xsl:value-of select="@version" /></td></tr>
        <tr><td class="c0">Acceptable Coverage:</td><td class="c1"><xsl:value-of select="project/@acceptable" />%</td></tr>
        <tr><td class="c0">&#32;</td><td class="c1"></td></tr>
      </table>
    </div>
  </xsl:template>

  <!-- content -->
  <xsl:template match="namespaces|modules" mode="content">
    <xsl:if test="module/namespace|namespace/class">
      <div class="links">
        <a href="#" onclick="var coll = document.getElementsByTagName('span'); for(i=0; i&lt;coll.length;i++) if(coll[i].className == 'child') coll[i].style.display = 'block'; return false;">Expand All</a> /
        <a href="#" onclick="var coll = document.getElementsByTagName('span'); for(i=0; i&lt;coll.length;i++) if(coll[i].className == 'child') coll[i].style.display = 'none'; return false;">Collapse All</a>
      </div>
    </xsl:if>
    <div id="content" class="modules content">
      <div class="header"><xsl:value-of select="local-name()"/></div>
      <xsl:apply-templates select="module|namespace" mode="list" />
    </div>
  </xsl:template>

  <!-- list -->
  <xsl:template match="module|namespace|class" mode="list">
    <xsl:param name="children" select="namespace|class" />
    <xsl:element name="div">
      <xsl:attribute name="class">list</xsl:attribute>
      <xsl:if test="$children">
        <xsl:attribute name="style">cursor: pointer;</xsl:attribute>
        <xsl:attribute name="onclick">this.nextSibling.style.display = this.nextSibling.style.display == 'none' ? 'block' : 'none';</xsl:attribute>
      </xsl:if>
      <xsl:element name="div">
        <xsl:attribute name="class">
          name <xsl:if test="$children">bold</xsl:if>
        </xsl:attribute>
        <!--<xsl:if test="$children">
          <div class="type"><xsl:value-of select="local-name(.)"/>&#32;</div>
        </xsl:if>-->
        <xsl:value-of select="@name"/>&#32;
      </xsl:element>
      <xsl:call-template name="bargraph">
        <xsl:with-param name="percent" select="@coverage" />
        <xsl:with-param name="total" select="@sequencePoints" />
        <xsl:with-param name="remain" select="@unvisitedPoints" />
      </xsl:call-template>
      <br />
    </xsl:element>
    <xsl:if test="$children">
      <span class="child" style="display: none;">
        <xsl:apply-templates select="namespace|class" mode="list" />
      </span>
    </xsl:if>
  </xsl:template>

  <!-- bargraph -->
  <xsl:template name="bargraph">
    <xsl:param name="percent" />
    <xsl:param name="total" />
    <xsl:param name="remain" />
    <xsl:param name="visited" select="$total - $remain" />
    
    <xsl:element name="div">
      <xsl:attribute name="class">bargraph</xsl:attribute>
      <xsl:attribute name="title">
        <xsl:value-of select="round($percent)" />% coverage (<xsl:value-of select="$visited" /> of <xsl:value-of select="$total" />, <xsl:value-of select="$remain" /> remaining)
      </xsl:attribute>
      <xsl:element name="div">
        <xsl:attribute name="class">
          <xsl:choose>
            <xsl:when test="number($percent) = 100">barfull</xsl:when>
            <xsl:when test="number($percent) &gt; number(/coverageReport/project/@acceptable)">bargood</xsl:when>
            <xsl:otherwise>barbad</xsl:otherwise>
          </xsl:choose>
        </xsl:attribute>
        <xsl:if test="number($percent) &gt; 0 and number($percent) &lt; 100">
          <xsl:element name="div">
            <xsl:attribute name="class">barpass</xsl:attribute>
            <xsl:attribute name="style">
              width: <xsl:value-of select="round($percent)" />%
            </xsl:attribute>
          </xsl:element>
        </xsl:if>
      </xsl:element>
      <xsl:value-of select="round($percent)" />%
    </xsl:element>
  </xsl:template>
  
</xsl:stylesheet>