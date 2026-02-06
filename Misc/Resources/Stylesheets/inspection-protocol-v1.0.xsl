<?xml version="1.0" encoding="UTF-8"?>
<!--
  ============================================================================
  ROBIGOO FIELD SPRAYER CONTROL STATION
  XSL-FO Stylesheet for Inspection Protocol PDF Generation
  ============================================================================
  
  File:        inspection-protocol-v1.0.xsl
  Version:     1.0
  Created:     2026-02-05
  Author:      Wojciech Salamon <wojciech.salamon@yahoo.com>
  
  Description:
    XSL-FO 1.0 stylesheet for generating A4 PDF documents from inspection
    protocol XML data. Based on SKO (Stacje Kontroli Opryskiwaczy) official
    protocol format for field sprayer technical inspection reports.
  
  Compatible with:
    - Apache FOP 2.x
    - Any XSL-FO 1.0 compliant processor
  
  Copyright (c) 2026 Wojciech Salamon. All rights reserved.
  ============================================================================
-->
<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:fo="http://www.w3.org/1999/XSL/Format">

  <!-- Stylesheet metadata -->
  <xsl:variable name="xsl-version">1.0</xsl:variable>
  <xsl:variable name="xsl-name">inspection-protocol</xsl:variable>
  <xsl:variable name="xsl-full-version">1.0</xsl:variable>

  <xsl:output method="xml" indent="yes"/>

  <!-- ============================================================
       MAIN TEMPLATE
       ============================================================ -->
  <xsl:template match="/InspectionProtocol">
    <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
      
      <!-- Page Layout for A4 -->
      <fo:layout-master-set>
        <fo:simple-page-master master-name="A4-page"
            page-width="210mm" page-height="297mm"
            margin-top="15mm" margin-bottom="15mm"
            margin-left="15mm" margin-right="15mm">
          <fo:region-body margin-top="10mm" margin-bottom="15mm"/>
          <fo:region-before extent="10mm"/>
          <fo:region-after extent="12mm"/>
        </fo:simple-page-master>
      </fo:layout-master-set>

      <!-- Document Content -->
      <fo:page-sequence master-reference="A4-page">
        
        <!-- Header -->
        <fo:static-content flow-name="xsl-region-before">
          <fo:block font-size="7pt" color="#666666" text-align="center">
            PROTOKÓŁ BADANIA TECHNICZNEGO OPRYSKIWACZA — Nr <xsl:value-of select="ProtocolNumber"/>
          </fo:block>
        </fo:static-content>

        <!-- Footer -->
        <fo:static-content flow-name="xsl-region-after">
          <fo:block font-size="7pt" color="#666666" text-align="center" border-top="0.5pt solid #cccccc" padding-top="3mm">
            Strona <fo:page-number/> z <fo:page-number-citation ref-id="last-page"/>
            <fo:block/>
            Wygenerowano: <xsl:value-of select="GeneratedAt"/> | Wersja szablonu XSL: <xsl:value-of select="$xsl-full-version"/>
          </fo:block>
        </fo:static-content>

        <!-- Main Content -->
        <fo:flow flow-name="xsl-region-body">
          
          <!-- DOCUMENT TITLE -->
          <fo:block font-size="14pt" font-weight="bold" text-align="center" 
                    space-after="3mm" color="#1a5f7a">
            PROTOKÓŁ BADANIA TECHNICZNEGO
          </fo:block>
          <fo:block font-size="11pt" font-weight="bold" text-align="center" 
                    space-after="8mm" color="#1a5f7a">
            OPRYSKIWACZA <xsl:choose>
              <xsl:when test="CropSprayerType = '00'">POLOWEGO</xsl:when>
              <xsl:when test="CropSprayerType = '01'">SADOWNICZEGO</xsl:when>
              <xsl:otherwise>POLOWEGO/SADOWNICZEGO</xsl:otherwise>
            </xsl:choose>
          </fo:block>

          <!-- PROTOCOL INFO BOX -->
          <fo:table width="100%" border="1pt solid #1a5f7a" space-after="5mm">
            <fo:table-column column-width="50%"/>
            <fo:table-column column-width="50%"/>
            <fo:table-body>
              <fo:table-row background-color="#e8f4f8">
                <fo:table-cell padding="2mm">
                  <fo:block font-size="9pt">
                    <fo:inline font-weight="bold">Nr protokołu: </fo:inline>
                    <xsl:value-of select="ProtocolNumber"/>
                  </fo:block>
                </fo:table-cell>
                <fo:table-cell padding="2mm">
                  <fo:block font-size="9pt">
                    <fo:inline font-weight="bold">Data badania: </fo:inline>
                    <xsl:value-of select="InspectionDate"/>
                  </fo:block>
                </fo:table-cell>
              </fo:table-row>
              <fo:table-row>
                <fo:table-cell padding="2mm" number-columns-spanned="2">
                  <fo:block font-size="9pt">
                    <fo:inline font-weight="bold">Miejsce badania: </fo:inline>
                    <xsl:value-of select="InspectionLocation"/>
                  </fo:block>
                </fo:table-cell>
              </fo:table-row>
            </fo:table-body>
          </fo:table>

          <!-- SECTION A: CLIENT DATA -->
          <xsl:call-template name="section-header">
            <xsl:with-param name="title">A. DANE WŁAŚCICIELA / POSIADACZA</xsl:with-param>
          </xsl:call-template>
          
          <fo:table width="100%" border="0.5pt solid #cccccc" space-after="5mm">
            <fo:table-column column-width="35%"/>
            <fo:table-column column-width="65%"/>
            <fo:table-body>
              <xsl:call-template name="data-row">
                <xsl:with-param name="label">Nazwa / Imię i nazwisko</xsl:with-param>
                <xsl:with-param name="value" select="ClientName"/>
              </xsl:call-template>
              <xsl:call-template name="data-row">
                <xsl:with-param name="label">Adres</xsl:with-param>
                <xsl:with-param name="value" select="ClientAddress"/>
              </xsl:call-template>
              <xsl:call-template name="data-row">
                <xsl:with-param name="label">NIP / PESEL</xsl:with-param>
                <xsl:with-param name="value" select="ClientTaxId"/>
              </xsl:call-template>
            </fo:table-body>
          </fo:table>

          <!-- SECTION B: SPRAYER DATA -->
          <xsl:call-template name="section-header">
            <xsl:with-param name="title">B. DANE OPRYSKIWACZA</xsl:with-param>
          </xsl:call-template>
          
          <fo:table width="100%" border="0.5pt solid #cccccc" space-after="5mm">
            <fo:table-column column-width="35%"/>
            <fo:table-column column-width="65%"/>
            <fo:table-body>
              <xsl:call-template name="data-row">
                <xsl:with-param name="label">Typ</xsl:with-param>
                <xsl:with-param name="value">
                  <xsl:choose>
                    <xsl:when test="CropSprayerType = '00'">Polowy</xsl:when>
                    <xsl:when test="CropSprayerType = '01'">Sadowniczy</xsl:when>
                    <xsl:otherwise><xsl:value-of select="CropSprayerType"/></xsl:otherwise>
                  </xsl:choose>
                </xsl:with-param>
              </xsl:call-template>
              <xsl:call-template name="data-row">
                <xsl:with-param name="label">Rodzaj</xsl:with-param>
                <xsl:with-param name="value">
                  <xsl:choose>
                    <xsl:when test="CropSprayerKind = '00'">Zawieszany</xsl:when>
                    <xsl:when test="CropSprayerKind = '01'">Przyczepiany</xsl:when>
                    <xsl:when test="CropSprayerKind = '02'">Samobieżny</xsl:when>
                    <xsl:when test="CropSprayerKind = '03'">Inny</xsl:when>
                    <xsl:otherwise><xsl:value-of select="CropSprayerKind"/></xsl:otherwise>
                  </xsl:choose>
                </xsl:with-param>
              </xsl:call-template>
              <xsl:call-template name="data-row">
                <xsl:with-param name="label">Nazwa / Model</xsl:with-param>
                <xsl:with-param name="value" select="CropSprayerName"/>
              </xsl:call-template>
              <xsl:call-template name="data-row">
                <xsl:with-param name="label">Producent</xsl:with-param>
                <xsl:with-param name="value" select="CropSprayerManufacturer"/>
              </xsl:call-template>
              <xsl:call-template name="data-row">
                <xsl:with-param name="label">Numer seryjny</xsl:with-param>
                <xsl:with-param name="value" select="CropSprayerSerialNumber"/>
              </xsl:call-template>
              <xsl:call-template name="data-row">
                <xsl:with-param name="label">Rok produkcji</xsl:with-param>
                <xsl:with-param name="value" select="CropSprayerProductionYear"/>
              </xsl:call-template>
              <xsl:call-template name="data-row">
                <xsl:with-param name="label">Pojemność zbiornika</xsl:with-param>
                <xsl:with-param name="value"><xsl:value-of select="TankCapacity"/> l</xsl:with-param>
              </xsl:call-template>
              <xsl:call-template name="data-row">
                <xsl:with-param name="label">Szerokość robocza belki</xsl:with-param>
                <xsl:with-param name="value"><xsl:value-of select="BoomWidth"/> m</xsl:with-param>
              </xsl:call-template>
              <xsl:call-template name="data-row">
                <xsl:with-param name="label">Liczba sekcji</xsl:with-param>
                <xsl:with-param name="value" select="SectionCount"/>
              </xsl:call-template>
            </fo:table-body>
          </fo:table>

          <!-- SECTION C: INSPECTION RESULTS -->
          <xsl:call-template name="section-header">
            <xsl:with-param name="title">C. WYNIKI BADANIA</xsl:with-param>
          </xsl:call-template>

          <!-- Inspection Results Table -->
          <fo:table width="100%" border="0.5pt solid #333333" space-after="5mm">
            <fo:table-column column-width="8%"/>
            <fo:table-column column-width="62%"/>
            <fo:table-column column-width="15%"/>
            <fo:table-column column-width="15%"/>
            <fo:table-header>
              <fo:table-row background-color="#1a5f7a" color="white" font-weight="bold">
                <fo:table-cell padding="2mm" border="0.5pt solid #333333">
                  <fo:block font-size="8pt" text-align="center">Lp.</fo:block>
                </fo:table-cell>
                <fo:table-cell padding="2mm" border="0.5pt solid #333333">
                  <fo:block font-size="8pt">Przedmiot badania</fo:block>
                </fo:table-cell>
                <fo:table-cell padding="2mm" border="0.5pt solid #333333">
                  <fo:block font-size="8pt" text-align="center">Wynik</fo:block>
                </fo:table-cell>
                <fo:table-cell padding="2mm" border="0.5pt solid #333333">
                  <fo:block font-size="8pt" text-align="center">Uwagi</fo:block>
                </fo:table-cell>
              </fo:table-row>
            </fo:table-header>
            <fo:table-body>
              
              <!-- Section 1: General -->
              <xsl:call-template name="section-title-row">
                <xsl:with-param name="number">1</xsl:with-param>
                <xsl:with-param name="title">STAN OGÓLNY OPRYSKIWACZA</xsl:with-param>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">1.1</xsl:with-param>
                <xsl:with-param name="item">Stan techniczny ogólny i czystość</xsl:with-param>
                <xsl:with-param name="passed" select="GeneralConditionPassed"/>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">1.2</xsl:with-param>
                <xsl:with-param name="item">Czytelność oznaczeń i tabliczek</xsl:with-param>
                <xsl:with-param name="passed" select="MarkingsReadablePassed"/>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">1.3</xsl:with-param>
                <xsl:with-param name="item">Kompletność wyposażenia</xsl:with-param>
                <xsl:with-param name="passed" select="EquipmentCompletePassed"/>
                <xsl:with-param name="notes" select="GeneralSectionNotes"/>
              </xsl:call-template>

              <!-- Section 2: Pump -->
              <xsl:call-template name="section-title-row">
                <xsl:with-param name="number">2</xsl:with-param>
                <xsl:with-param name="title">POMPA</xsl:with-param>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">2.1</xsl:with-param>
                <xsl:with-param name="item">Praca pompy i napędu</xsl:with-param>
                <xsl:with-param name="passed" select="PumpOperationPassed"/>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">2.2</xsl:with-param>
                <xsl:with-param name="item">Szczelność pompy</xsl:with-param>
                <xsl:with-param name="passed" select="PumpSealingPassed"/>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">2.3</xsl:with-param>
                <xsl:with-param name="item">Pulsacje ciśnienia</xsl:with-param>
                <xsl:with-param name="passed" select="PressurePulsationPassed"/>
                <xsl:with-param name="notes" select="PumpSectionNotes"/>
              </xsl:call-template>

              <!-- Section 3: Agitation -->
              <xsl:call-template name="section-title-row">
                <xsl:with-param name="number">3</xsl:with-param>
                <xsl:with-param name="title">MIESZALNIK</xsl:with-param>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">3.1</xsl:with-param>
                <xsl:with-param name="item">Sprawność mieszalnika</xsl:with-param>
                <xsl:with-param name="passed" select="AgitatorOperationPassed"/>
                <xsl:with-param name="notes" select="AgitatorSectionNotes"/>
              </xsl:call-template>

              <!-- Section 4: Tank -->
              <xsl:call-template name="section-title-row">
                <xsl:with-param name="number">4</xsl:with-param>
                <xsl:with-param name="title">ZBIORNIK</xsl:with-param>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">4.1</xsl:with-param>
                <xsl:with-param name="item">Stan zbiornika</xsl:with-param>
                <xsl:with-param name="passed" select="TankConditionPassed"/>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">4.2</xsl:with-param>
                <xsl:with-param name="item">Szczelność zbiornika</xsl:with-param>
                <xsl:with-param name="passed" select="TankSealingPassed"/>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">4.3</xsl:with-param>
                <xsl:with-param name="item">Wskaźnik poziomu cieczy</xsl:with-param>
                <xsl:with-param name="passed" select="LevelIndicatorPassed"/>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">4.4</xsl:with-param>
                <xsl:with-param name="item">Układ płukania</xsl:with-param>
                <xsl:with-param name="passed" select="FlushingSystemPassed"/>
                <xsl:with-param name="notes" select="TankSectionNotes"/>
              </xsl:call-template>

              <!-- Section 5: Measuring -->
              <xsl:call-template name="section-title-row">
                <xsl:with-param name="number">5</xsl:with-param>
                <xsl:with-param name="title">PRZYRZĄDY POMIAROWE (MANOMETR)</xsl:with-param>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">5.1</xsl:with-param>
                <xsl:with-param name="item">Stan i czytelność manometru</xsl:with-param>
                <xsl:with-param name="passed" select="ManometerPassed"/>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">5.2</xsl:with-param>
                <xsl:with-param name="item">Średnica tarczy (min 63mm)</xsl:with-param>
                <xsl:with-param name="passed" select="ManometerDialSizePassed"/>
              </xsl:call-template>
              <fo:table-row>
                <fo:table-cell padding="1.5mm" border="0.5pt solid #cccccc">
                  <fo:block font-size="7pt" text-align="center">5.3</fo:block>
                </fo:table-cell>
                <fo:table-cell padding="1.5mm" border="0.5pt solid #cccccc" number-columns-spanned="3">
                  <fo:block font-size="8pt">
                    Odczyty manometru: 
                    2 bar → <fo:inline font-weight="bold"><xsl:value-of select="ManometerReading2Bar"/> bar</fo:inline> | 
                    4 bar → <fo:inline font-weight="bold"><xsl:value-of select="ManometerReading4Bar"/> bar</fo:inline> | 
                    6 bar → <fo:inline font-weight="bold"><xsl:value-of select="ManometerReading6Bar"/> bar</fo:inline>
                  </fo:block>
                </fo:table-cell>
              </fo:table-row>

              <!-- Section 6: Piping -->
              <xsl:call-template name="section-title-row">
                <xsl:with-param name="number">6</xsl:with-param>
                <xsl:with-param name="title">PRZEWODY</xsl:with-param>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">6.1</xsl:with-param>
                <xsl:with-param name="item">Stan przewodów i węży</xsl:with-param>
                <xsl:with-param name="passed" select="PipesConditionPassed"/>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">6.2</xsl:with-param>
                <xsl:with-param name="item">Szczelność połączeń</xsl:with-param>
                <xsl:with-param name="passed" select="ConnectionsSealingPassed"/>
                <xsl:with-param name="notes" select="PipingSectionNotes"/>
              </xsl:call-template>

              <!-- Section 7: Filtration -->
              <xsl:call-template name="section-title-row">
                <xsl:with-param name="number">7</xsl:with-param>
                <xsl:with-param name="title">FILTRACJA</xsl:with-param>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">7.1</xsl:with-param>
                <xsl:with-param name="item">Filtr ssawny</xsl:with-param>
                <xsl:with-param name="passed" select="SuctionFilterPassed"/>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">7.2</xsl:with-param>
                <xsl:with-param name="item">Filtr ciśnieniowy (tłoczny)</xsl:with-param>
                <xsl:with-param name="passed" select="PressureFilterPassed"/>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">7.3</xsl:with-param>
                <xsl:with-param name="item">Filtry rozpylaczy</xsl:with-param>
                <xsl:with-param name="passed" select="NozzleFiltersPassed"/>
                <xsl:with-param name="notes" select="FiltrationSectionNotes"/>
              </xsl:call-template>

              <!-- Section 8: Boom -->
              <xsl:call-template name="section-title-row">
                <xsl:with-param name="number">8</xsl:with-param>
                <xsl:with-param name="title">BELKA / URZĄDZENIE ROZPYLAJĄCE</xsl:with-param>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">8.1</xsl:with-param>
                <xsl:with-param name="item">Stan belki polowej</xsl:with-param>
                <xsl:with-param name="passed" select="FieldBoomConditionPassed"/>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">8.2</xsl:with-param>
                <xsl:with-param name="item">Stabilność belki</xsl:with-param>
                <xsl:with-param name="passed" select="BoomStabilityPassed"/>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">8.3</xsl:with-param>
                <xsl:with-param name="item">Wysokość belki</xsl:with-param>
                <xsl:with-param name="passed" select="BoomHeightPassed"/>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">8.4</xsl:with-param>
                <xsl:with-param name="item">Symetria belki</xsl:with-param>
                <xsl:with-param name="passed" select="BoomSymmetryPassed"/>
                <xsl:with-param name="notes" select="BoomSectionNotes"/>
              </xsl:call-template>

              <!-- Section 9: Nozzles -->
              <xsl:call-template name="section-title-row">
                <xsl:with-param name="number">9</xsl:with-param>
                <xsl:with-param name="title">ROZPYLACZE</xsl:with-param>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">9.1</xsl:with-param>
                <xsl:with-param name="item">Jednorodność rozpylaczy</xsl:with-param>
                <xsl:with-param name="passed" select="NozzleUniformityPassed"/>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">9.2</xsl:with-param>
                <xsl:with-param name="item">Wydatek rozpylaczy</xsl:with-param>
                <xsl:with-param name="passed" select="NozzleFlowRatePassed"/>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">9.3</xsl:with-param>
                <xsl:with-param name="item">Stan rozpylaczy</xsl:with-param>
                <xsl:with-param name="passed" select="NozzleConditionPassed"/>
                <xsl:with-param name="notes" select="NozzlesSectionNotes"/>
              </xsl:call-template>

              <!-- Section 10: Distribution -->
              <xsl:call-template name="section-title-row">
                <xsl:with-param name="number">10</xsl:with-param>
                <xsl:with-param name="title">RÓWNOMIERNOŚĆ ROZPROWADZANIA</xsl:with-param>
              </xsl:call-template>
              <xsl:call-template name="check-row">
                <xsl:with-param name="number">10.1</xsl:with-param>
                <xsl:with-param name="item">Poprzeczna równomierność rozprowadzania</xsl:with-param>
                <xsl:with-param name="passed" select="TransverseDistributionPassed"/>
              </xsl:call-template>
              <fo:table-row>
                <fo:table-cell padding="1.5mm" border="0.5pt solid #cccccc">
                  <fo:block font-size="7pt" text-align="center">10.2</fo:block>
                </fo:table-cell>
                <fo:table-cell padding="1.5mm" border="0.5pt solid #cccccc" number-columns-spanned="3">
                  <fo:block font-size="8pt">
                    Współczynnik zmienności CV: <fo:inline font-weight="bold"><xsl:value-of select="CoefficientOfVariation"/> %</fo:inline>
                    <xsl:if test="DistributionSectionNotes">
                      <fo:block font-size="7pt" color="#666666">
                        Uwagi: <xsl:value-of select="DistributionSectionNotes"/>
                      </fo:block>
                    </xsl:if>
                  </fo:block>
                </fo:table-cell>
              </fo:table-row>

            </fo:table-body>
          </fo:table>

          <!-- SECTION D: FINAL RESULT -->
          <xsl:call-template name="section-header">
            <xsl:with-param name="title">D. WYNIK KOŃCOWY BADANIA</xsl:with-param>
          </xsl:call-template>

          <fo:table width="100%" border="2pt solid #1a5f7a" space-after="5mm">
            <fo:table-column column-width="100%"/>
            <fo:table-body>
              <fo:table-row>
                <fo:table-cell padding="4mm" text-align="center">
                  <xsl:attribute name="background-color">
                    <xsl:choose>
                      <xsl:when test="FinalResult = 'true'">#d4edda</xsl:when>
                      <xsl:otherwise>#f8d7da</xsl:otherwise>
                    </xsl:choose>
                  </xsl:attribute>
                  <fo:block font-size="16pt" font-weight="bold">
                    <xsl:attribute name="color">
                      <xsl:choose>
                        <xsl:when test="FinalResult = 'true'">#155724</xsl:when>
                        <xsl:otherwise>#721c24</xsl:otherwise>
                      </xsl:choose>
                    </xsl:attribute>
                    <xsl:choose>
                      <xsl:when test="FinalResult = 'true'">WYNIK POZYTYWNY</xsl:when>
                      <xsl:otherwise>WYNIK NEGATYWNY</xsl:otherwise>
                    </xsl:choose>
                  </fo:block>
                </fo:table-cell>
              </fo:table-row>
            </fo:table-body>
          </fo:table>

          <xsl:if test="FinalResult = 'true'">
            <fo:table width="100%" border="0.5pt solid #cccccc" space-after="5mm">
              <fo:table-column column-width="35%"/>
              <fo:table-column column-width="65%"/>
              <fo:table-body>
                <xsl:call-template name="data-row">
                  <xsl:with-param name="label">Data ważności badania</xsl:with-param>
                  <xsl:with-param name="value" select="ValidUntil"/>
                </xsl:call-template>
                <xsl:call-template name="data-row">
                  <xsl:with-param name="label">Numer nalepki kontrolnej</xsl:with-param>
                  <xsl:with-param name="value" select="ControlStickerNumber"/>
                </xsl:call-template>
              </fo:table-body>
            </fo:table>
          </xsl:if>

          <xsl:if test="GeneralNotes">
            <fo:block font-size="9pt" space-before="3mm" space-after="5mm" 
                      padding="3mm" background-color="#fff3cd" border="0.5pt solid #ffc107">
              <fo:block font-weight="bold" space-after="1mm">Uwagi ogólne / Zalecenia:</fo:block>
              <xsl:value-of select="GeneralNotes"/>
            </fo:block>
          </xsl:if>

          <!-- SECTION E: SIGNATURES -->
          <xsl:call-template name="section-header">
            <xsl:with-param name="title">E. PODPISY</xsl:with-param>
          </xsl:call-template>

          <fo:table width="100%" space-after="5mm">
            <fo:table-column column-width="50%"/>
            <fo:table-column column-width="50%"/>
            <fo:table-body>
              <fo:table-row>
                <fo:table-cell padding="3mm">
                  <fo:block font-size="9pt" text-align="center" space-after="15mm">
                    <fo:block font-weight="bold">Diagnosta:</fo:block>
                    <fo:block><xsl:value-of select="InspectorName"/></fo:block>
                    <fo:block font-size="8pt" color="#666666">Nr uprawnień: <xsl:value-of select="InspectorLicenseNumber"/></fo:block>
                  </fo:block>
                  <fo:block text-align="center" border-top="0.5pt solid #333333" padding-top="2mm" font-size="8pt">
                    (podpis diagnosty)
                  </fo:block>
                </fo:table-cell>
                <fo:table-cell padding="3mm">
                  <fo:block font-size="9pt" text-align="center" space-after="15mm">
                    <fo:block font-weight="bold">Właściciel / Posiadacz:</fo:block>
                    <fo:block><xsl:value-of select="ClientName"/></fo:block>
                    <fo:block>&#160;</fo:block>
                  </fo:block>
                  <fo:block text-align="center" border-top="0.5pt solid #333333" padding-top="2mm" font-size="8pt">
                    (podpis właściciela/posiadacza)
                  </fo:block>
                </fo:table-cell>
              </fo:table-row>
            </fo:table-body>
          </fo:table>

          <!-- Last page marker for page numbering -->
          <fo:block id="last-page"/>
          
        </fo:flow>
      </fo:page-sequence>
    </fo:root>
  </xsl:template>

  <!-- ============================================================
       HELPER TEMPLATES
       ============================================================ -->

  <!-- Section Header Template -->
  <xsl:template name="section-header">
    <xsl:param name="title"/>
    <fo:block font-size="10pt" font-weight="bold" color="white" 
              background-color="#1a5f7a" padding="2mm" space-after="2mm">
      <xsl:value-of select="$title"/>
    </fo:block>
  </xsl:template>

  <!-- Data Row Template -->
  <xsl:template name="data-row">
    <xsl:param name="label"/>
    <xsl:param name="value"/>
    <fo:table-row>
      <fo:table-cell padding="2mm" border="0.5pt solid #cccccc" background-color="#f8f9fa">
        <fo:block font-size="8pt" font-weight="bold"><xsl:value-of select="$label"/></fo:block>
      </fo:table-cell>
      <fo:table-cell padding="2mm" border="0.5pt solid #cccccc">
        <fo:block font-size="9pt"><xsl:value-of select="$value"/></fo:block>
      </fo:table-cell>
    </fo:table-row>
  </xsl:template>

  <!-- Section Title Row Template -->
  <xsl:template name="section-title-row">
    <xsl:param name="number"/>
    <xsl:param name="title"/>
    <fo:table-row background-color="#e8f4f8">
      <fo:table-cell padding="2mm" border="0.5pt solid #cccccc" font-weight="bold">
        <fo:block font-size="8pt" text-align="center"><xsl:value-of select="$number"/></fo:block>
      </fo:table-cell>
      <fo:table-cell padding="2mm" border="0.5pt solid #cccccc" number-columns-spanned="3" font-weight="bold">
        <fo:block font-size="8pt"><xsl:value-of select="$title"/></fo:block>
      </fo:table-cell>
    </fo:table-row>
  </xsl:template>

  <!-- Check Row Template -->
  <xsl:template name="check-row">
    <xsl:param name="number"/>
    <xsl:param name="item"/>
    <xsl:param name="passed"/>
    <xsl:param name="notes"/>
    <fo:table-row>
      <fo:table-cell padding="1.5mm" border="0.5pt solid #cccccc">
        <fo:block font-size="7pt" text-align="center"><xsl:value-of select="$number"/></fo:block>
      </fo:table-cell>
      <fo:table-cell padding="1.5mm" border="0.5pt solid #cccccc">
        <fo:block font-size="8pt"><xsl:value-of select="$item"/></fo:block>
      </fo:table-cell>
      <fo:table-cell padding="1.5mm" border="0.5pt solid #cccccc" text-align="center">
        <xsl:attribute name="background-color">
          <xsl:choose>
            <xsl:when test="$passed = 'true'">#d4edda</xsl:when>
            <xsl:when test="$passed = 'false'">#f8d7da</xsl:when>
            <xsl:otherwise>#ffffff</xsl:otherwise>
          </xsl:choose>
        </xsl:attribute>
        <fo:block font-size="10pt" font-weight="bold">
          <xsl:choose>
            <xsl:when test="$passed = 'true'">✓</xsl:when>
            <xsl:when test="$passed = 'false'">✗</xsl:when>
            <xsl:otherwise>—</xsl:otherwise>
          </xsl:choose>
        </fo:block>
      </fo:table-cell>
      <fo:table-cell padding="1.5mm" border="0.5pt solid #cccccc">
        <fo:block font-size="7pt" color="#666666">
          <xsl:if test="$notes"><xsl:value-of select="$notes"/></xsl:if>
        </fo:block>
      </fo:table-cell>
    </fo:table-row>
  </xsl:template>

</xsl:stylesheet>
