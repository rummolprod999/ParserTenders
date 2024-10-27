using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;
using HtmlAgilityPack;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParserTenders.TenderDir;

namespace ParserTenders.ParserDir
{
    public class ParserTendersWeb : ParserWeb
    {
        private const int PageCount = 10;

        private readonly List<string> _listUrls = new List<string>
        {
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277317&customerPlaceCodes=OKER30&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            $"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&publishDateFrom={DateTime.Now.AddDays(-1):dd.MM.yyyy}+-+{DateTime.Now.AddDays(+1):dd.MM.yyyy}&publishDateTo={DateTime.Now.AddDays(+1):dd.MM.yyyy}&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277336&customerPlaceCodes=OKER31&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&F=on&S=on&M=on&NOT_FSM=on&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=UPDATE_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&F=on&S=on&M=on&NOT_FSM=on&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277377&customerPlaceCodes=OKER34&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=9409197&customerPlaceCodes=OKER38&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277399&customerPlaceCodes=OKER36&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277384&customerPlaceCodes=OKER35&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277362&customerPlaceCodes=OKER33&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=9371527&customerPlaceCodes=OKER40&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277409&customerPlaceCodes=OKER40&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2IdsWithNested=on&okpd2IdsSeveral=on&okpd2Ids=8873863%2C8873862%2C8873861%2C8873871%2C8873870%2C8873869%2C8873868%2C8873867%2C8873866%2C8873865%2C8873864%2C8873879%2C8873878%2C8873877%2C8873876%2C8873875%2C8873874%2C8873873%2C8873872%2C8873881%2C8873880&okpd2IdsCodes=C%2CB%2CA%2CK%2CJ%2CI%2CH%2CG%2CF%2CE%2CD%2CS%2CR%2CQ%2CP%2CO%2CN%2CM%2CL%2CU%2CT&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&delKladrIdsWithNested=on&delKladrIds=5277409%2C5277377%2C9409197%2C5277317%2C5277399&delKladrIdsCodes=99000000000%2COKER34%2COKER38%2COKER30%2COKER36&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&delKladrIdsWithNested=on&delKladrIds=5277362%2C5277336%2C5277384%2C9371527&delKladrIdsCodes=OKER33%2COKER31%2COKER35%2COKER40&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            $"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=UPDATE_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom={DateTime.Now.AddDays(-1):dd.MM.yyyy}+-+{DateTime.Now.AddDays(+1):dd.MM.yyyy}&publishDateTo={DateTime.Now.AddDays(+1):dd.MM.yyyy}&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            $"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom={DateTime.Now:dd.MM.yyyy}+-+{DateTime.Now:dd.MM.yyyy}&publishDateTo={DateTime.Now:dd.MM.yyyy}&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            $"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=true&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom={DateTime.Now:dd.MM.yyyy}+-+{DateTime.Now:dd.MM.yyyy}&publishDateTo={DateTime.Now:dd.MM.yyyy}&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=UPDATE_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            $"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=UPDATE_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&updateDateFrom={DateTime.Now.AddDays(-1):dd.MM.yyyy}+-+{DateTime.Now:dd.MM.yyyy}&updateDateTo={DateTime.Now:dd.MM.yyyy}&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            $"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=true&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=UPDATE_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&updateDateFrom={DateTime.Now.AddDays(-1):dd.MM.yyyy}+-+{DateTime.Now:dd.MM.yyyy}&updateDateTo={DateTime.Now:dd.MM.yyyy}&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            $"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PRICE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&updateDateFrom={DateTime.Now.AddDays(-1):dd.MM.yyyy}+-+{DateTime.Now:dd.MM.yyyy}&updateDateTo={DateTime.Now:dd.MM.yyyy}&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            $"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=true&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PRICE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&updateDateFrom={DateTime.Now.AddDays(-1):dd.MM.yyyy}+-+{DateTime.Now:dd.MM.yyyy}&updateDateTo={DateTime.Now:dd.MM.yyyy}&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            $"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=RELEVANCE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&updateDateFrom={DateTime.Now.AddDays(-1):dd.MM.yyyy}+-+{DateTime.Now:dd.MM.yyyy}&updateDateTo={DateTime.Now:dd.MM.yyyy}&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            $"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=RELEVANCE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom={DateTime.Now.AddDays(-1):dd.MM.yyyy}+-+{DateTime.Now:dd.MM.yyyy}&publishDateTo={DateTime.Now:dd.MM.yyyy}&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            $"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=true&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=RELEVANCE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom={DateTime.Now.AddDays(-1):dd.MM.yyyy}+-+{DateTime.Now:dd.MM.yyyy}&publishDateTo={DateTime.Now:dd.MM.yyyy}&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            $"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=true&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PRICE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom={DateTime.Now.AddDays(-1):dd.MM.yyyy}+-+{DateTime.Now:dd.MM.yyyy}&publishDateTo={DateTime.Now:dd.MM.yyyy}&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            $"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PRICE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom={DateTime.Now.AddDays(-1):dd.MM.yyyy}+-+{DateTime.Now:dd.MM.yyyy}&publishDateTo={DateTime.Now:dd.MM.yyyy}&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber="
        };

        public ParserTendersWeb(TypeArguments a) : base(a)
        {
        }

        public override void Parsing()
        {
            _listUrls.ForEach(ParsingPage);
        }

        private void ParsingPage(string u)
        {
            var maxP = MaxPage(Uri.EscapeUriString($"{u}1"));
            for (var i = 1; i <= maxP; i++)
            {
                var url =
                    Uri.EscapeUriString($"{u}{i}");
                try
                {
                    ParserPage(url);
                }
                catch (Exception e)
                {
                    Log.Logger("Error in ParserTendersWeb.ParserPage", e);
                }
            }
        }

        private void ParserPage(string url)
        {
            if (DownloadString.MaxDownload > 1000) return;
            var s = DownloadString.DownLUserAgentEis(url);
            if (string.IsNullOrEmpty(s))
            {
                Log.Logger("Empty string in ParserPage()", url);
                return;
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(s);
            var tens = htmlDoc.DocumentNode.SelectNodes(
                           "//div[contains(@class, 'search-registry-entry-block')]/div[contains(@class, 'row')][1]") ??
                       new HtmlNodeCollection(null);
            foreach (var a in tens)
            {
                try
                {
                    ParserLink(a);
                }
                catch (Exception e)
                {
                    Log.Logger(e);
                }
            }
        }

        private void ParserLink(HtmlNode n)
        {
            if (DownloadString.MaxDownload > 1000) return;
            var urlT =
                (n.SelectSingleNode(".//div[contains(@class, 'registry-entry__header-mid__number')]/a")?.Attributes["href"]?.Value ?? "").Trim();
            var u = "https://zakupki.gov.ru/" + urlT;
            var st = DownloadString.DownLUserAgentEis(u);
            if (string.IsNullOrEmpty(st))
            {
                Log.Logger("Empty string in ParserPage()", u);
                return;
            }

            var htmlDocT = new HtmlDocument();
            htmlDocT.LoadHtml(st);
            var url =
                (htmlDocT.DocumentNode.SelectSingleNode(".//a[contains(@href, 'print-form')]")?.Attributes["href"]?.Value ?? "").Trim();
            if (!url.Contains("223/purchase")) return;
            var purNumT = (n.SelectSingleNode(".//div[contains(@class, 'registry-entry__header-mid__number')]/a")?.InnerText.Replace("№", "") ?? "").Trim();
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(purNumT)) return;
            var purNum = purNumT;
            if (purNum == "")
            {
                Log.Logger("purNum not found");
                return;
            }

            using (var connect = ConnectToDb.GetDbConnection())
            {
                connect.Open();
                var selectTender =
                    $"SELECT id_tender FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number AND type_fz = 223";
                var cmd = new MySqlCommand(selectTender, connect);
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@purchase_number", purNum);
                var reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Close();
                    return;
                }

                reader.Close();
            }
            if (DownloadString.MaxDownload > 1000) return;
            var s = DownloadString.DownLUserAgentEis(url);
            if (string.IsNullOrEmpty(s))
            {
                Log.Logger("Empty string in ParserLink()", url);
                return;
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(s);
            var xml = (htmlDoc.DocumentNode.SelectSingleNode("//div[@id= \"tabs-2\"]")?.InnerText ?? "").Trim();
            xml = WebUtility.HtmlDecode(xml);
            if (string.IsNullOrEmpty(xml))
            {
                Log.Logger("empty xml in ParserLink", url);
                return;
            }

            try
            {
                Parser223Web(xml, url);
            }
            catch (Exception e)
            {
                Log.Logger("Error in Parser223Web()", e);
            }
        }

        private void Parser223Web(string ftext, string url)
        {
            ftext = ClearText.ClearString(ftext);
            var doc = new XmlDocument();
            doc.LoadXml(ftext);
            var jsons = JsonConvert.SerializeXmlNode(doc);
            var json = JObject.Parse(jsons);
            if (ftext.Contains("purchaseNoticeZPESMBO"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeZpesmbo);
            }

            if (ftext.Contains("purchaseNoticeZKESMBO"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeZkesmbo);
            }

            if (ftext.Contains("purchaseNoticeKESMBO"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeKesmbo);
            }

            if (ftext.Contains("purchaseNoticeAESMBO"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeAesmbo);
            }

            if (ftext.Contains("purchaseNoticeZK"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeZk);
            }
            else if (ftext.Contains("purchaseNoticeOK"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeOk);
            }
            else if (ftext.Contains("purchaseNoticeOA"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeOa);
            }
            else if (ftext.Contains("purchaseNoticeIS"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeIs);
            }
            else if (ftext.Contains("purchaseNoticeEP"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeEp);
            }
            else if (ftext.Contains("purchaseNoticeAE94"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeAe94);
            }
            else if (ftext.Contains("purchaseNoticeAE"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeAe);
            }
            else if (ftext.Contains("purchaseNotice"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNotice);
            }
            else
            {
                Log.Logger("cannot find root tag in xml", url);
            }
        }

        public void Bolter223(string url, JObject json, TypeFile223 typefile)
        {
            try
            {
                var a = new TenderType223Web(url, json, typefile);
                a.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger("Ошибка при парсинге xml", e, url);
                Log.Logger(e.Source);
                Log.Logger(e.StackTrace);
            }
        }
    }
}