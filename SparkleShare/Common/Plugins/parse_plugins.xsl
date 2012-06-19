<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="xml" indent="yes" encoding="utf-8" />

  <!-- rename tags with underscore to name without underscore -->

  <xsl:template match="//_name">
    <name>
      <xsl:value-of select="."/>
    </name>
  </xsl:template>

  <xsl:template match="//_description">
    <description>
      <xsl:value-of select="."/>
    </description>
  </xsl:template>

  <xsl:template match="//_example">
    <example>
      <xsl:value-of select="."/>
    </example>
  </xsl:template>

  <!-- copy anything else -->
  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
  </xsl:template>

</xsl:stylesheet>

