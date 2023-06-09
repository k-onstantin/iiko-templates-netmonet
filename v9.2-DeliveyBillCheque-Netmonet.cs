@using System
@using System.Collections.Generic
@using System.Linq
@using Resto.Front.PrintTemplates.Cheques.Razor
@using Resto.Front.PrintTemplates.Cheques.Razor.TemplateModels
@using Resto.Front.PrintTemplates.RmsEntityWrappers
@using System.Text.RegularExpressions

@inherits TemplateBase<IDeliveryBillCheque>
<doc bell="">
    @Header()
    @Body()
    @Footer()
</doc>

@helper Header()
{
    var delivery = Model.Delivery;
    var customer = delivery.Customer;
    var address = delivery.Address;
    var cafeSetup = Model.CommonInfo.CafeSetup;
    var order = delivery.Order;

    <whitespace-preserve>@Raw(string.Join(Environment.NewLine, Model.Extensions.BeforeHeader))</whitespace-preserve>
    <left><split><whitespace-preserve>@cafeSetup.BillHeader</whitespace-preserve></split></left>
    <np />
    <center>@Resources.DeliveryBill</center>
    <center>@string.Format(Resources.BillHeaderOrderNumberPattern, delivery.Number)</center>

    if (!string.IsNullOrWhiteSpace(order.ExternalNumber))
    {
        <left><wrap>@string.Format(Resources.BillHeaderOrderExternalNumberPattern, order.ExternalNumber)</wrap></left>
    }

    <np />
    <left>@Resources.ConsignorColon</left>
    <left>@cafeSetup.LegalName</left>
    <left>@string.Format(Resources.HeadCafeTaxIdNoColonFormat, cafeSetup.TaxId)</left>
    <np />
    <left>@string.Format(Resources.ConsigneeColon)</left>
    <left>@customer.NameSurname()</left>
    <left>@delivery.Phone</left>
    <np />

    if (!delivery.IsSelfService)
    {
        <left><split>@string.Format(Resources.AddressFormat, address.StringView())</split></left>
        <left>@(string.IsNullOrWhiteSpace(address.AdditionalInfo) ? string.Empty : address.AdditionalInfo)</left>
        if (!string.IsNullOrWhiteSpace(address.Region))
        {
            <left>@string.Format(Resources.AddressRegionFormat, address.Region)</left>
        }
        <np />
    }

    if (!string.IsNullOrWhiteSpace(customer.CardNumber))
    {
        <left>@string.Format(Resources.DiscountCardNumber, customer.CardNumber)</left>
    }

    <table>
        <columns>
            <column autowidth="" />
            <column align="left" />
        </columns>
        <cells>
            <ct>@Resources.ManagerColon</ct>
            <ct>@Regex.Replace(Model.CommonInfo.CurrentUser.GetNameOrEmpty(), "\\d{6}", "")</ct>
            @if (!delivery.IsSelfService)
            {
                <ct>@Resources.CourierColon</ct>
                if (delivery.Courier != null)
                {
                    <ct>@Regex.Replace(delivery.Courier.Name, "\\d{6}", "")</ct>
                }
                else
                {
                    <c><line symbols="_" /></c>
                }
            }
        </cells>
    </table>
    <np />
    <table>
        <columns>
            <column autowidth="" />
            <column align="left" />
        </columns>
        <cells>
            <ct>@Resources.DeliveryCreatedColon</ct>
            <ct>@FormatDateTimeCustom(delivery.Created, "HH:mm dd.MM.yyyy")</ct>
            <ct>@Resources.DeliveryBillPrintedColon</ct>
            <ct>@FormatDateTimeCustom(Model.CommonInfo.CurrentTime, "HH:mm dd.MM.yyyy")</ct>
            <ct>@Resources.DeliveryDateTimeColon</ct>
            <ct>@FormatDateTimeCustom(delivery.DeliverTime, "HH:mm dd.MM.yyyy")</ct>
            <ct>@Resources.DeliveryRealDateTimeColon</ct>
            <c>________________</c>
        </cells>
    </table>
    <np />
    <whitespace-preserve>@Raw(string.Join(Environment.NewLine, Model.Extensions.AfterHeader))</whitespace-preserve>
    <np />
}

@helper Footer()
{
    var delivery = Model.Delivery;
    var cafeSetup = Model.CommonInfo.CafeSetup;
    var order = delivery.Order;
    var group = Model.CommonInfo.Group.Name;
    var terminal = Model.CommonInfo.CurrentTerminal;
    var transliterationMap = new Dictionary<string, string> {
        {"а", "a"},  {"б", "b"},  {"в", "v"},  {"г", "g"}, {"д", "d"},
        {"е", "e"},  {"ё", "e"},  {"ж", "zh"}, {"з", "z"}, {"и", "i"},
        {"й", "y"},  {"к", "k"},  {"л", "l"},  {"м", "m"}, {"н", "n"},
        {"о", "o"},  {"п", "p"},  {"р", "r"},  {"с", "s"}, {"т", "t"},
        {"у", "y"},  {"ф", "ph"}, {"х", "h"},  {"ц", "c"}, {"ч", "ch"},
        {"ш", "sh"}, {"щ", "sh"}, {"ы", "i"},  {"э", "e"}, {"ю", "u"},
        {"я", "ya"}, {"a", "a"},  {"b", "b"},  {"c", "c"}, {"d", "d"},
        {"e", "e"},  {"f", "f"},  {"g", "g"},  {"h", "h"}, {"i", "i"},
        {"j", "j"},  {"k", "k"},  {"l", "l"},  {"m", "m"}, {"n", "n"},
        {"o", "o"},  {"p", "p"},  {"q", "q"},  {"r", "r"}, {"s", "s"},
        {"t", "t"},  {"u", "u"},  {"v", "v"},  {"w", "w"}, {"x", "x"},
        {"y", "y"},  {"z", "z"}
        };

    var transliteratedName = string.Concat(delivery.Courier.GetNameOrEmpty().ToLower().Select(c => {
        string saveString;
    if (transliterationMap.TryGetValue(c.ToString(), out saveString)) {
        return saveString;
        } else {
            return "_";
            }
            }));

    if (delivery.IsSelfService) {
        transliteratedName = string.Concat(Model.CommonInfo.CurrentUser.GetNameOrEmpty().ToLower().Select(c => {
        string saveString;
        if (transliterationMap.TryGetValue(c.ToString(), out saveString)) {
            return saveString;
            } else {
                return "_";
                }
                }));
        }

    var fullSum = order.GetFullSum() - order.DiscountItems.Where(di => !di.Type.PrintProductItemInPrecheque).Sum(di => di.GetDiscountSum());

    var urls = new Dictionary<string, string> {
        {"source", "?o=3"},
        {"sum", string.Concat("&s=", fullSum)},
        {"number", string.Concat("&c=", order.Number)},
        {"table", string.Concat("&tn=", order.Table.Number)},
        {"name", string.Concat("&en=", transliteratedName)},
        {"wp", "&wpid="}
        };
    
    var code = "XXXXXX";         // код заведения
    var wpid = "XXXXXXX";        // WPID
                                 //
    bool useGroupCode = false;   // true для группового кода
    bool useGrouping = false;    // true для разделения по группам
    
    // groupDictionary заполняется только при useGrouping = true
    // указать имя группы вместо "Группа 1", "Группа 2" и тд.
    var groupDictionary = new Dictionary<string, Tuple<string, string, bool>> {        
        {"Группа 1", Tuple.Create(
            "XXXXXX",    // код заведения
            "XXXXXXX",   // WPID
            false        // true для группового кода
            )},
        {"Группа 2", Tuple.Create(
            "XXXXXX",    // код заведения
            "XXXXXXX",   // WPID
            false        // true для группового кода
            )}
        };

    var url = "https://netmonet.co/tip/";    
    var urlParams = string.Concat(urls["source"], urls["sum"], urls["number"], urls["table"], urls["name"]);

    if (useGrouping && groupDictionary.ContainsKey(group)) {
        code = groupDictionary[group].Item1;
        wpid = groupDictionary[group].Item2;
        useGroupCode = groupDictionary[group].Item3;
        }

    var codeMatches = Regex.Match(delivery.Courier.GetNameOrEmpty(), ".*(\\d{6}).*");
    if (delivery.IsSelfService) {
        codeMatches = Regex.Match(Model.CommonInfo.CurrentUser.GetNameOrEmpty(), ".*(\\d{6}).*");
        }
    bool codeFound = codeMatches.Groups.Count == 2;
    if (codeFound) {
        code = codeMatches.Groups[1].Value;
        urlParams = string.Concat(urls["source"], urls["sum"], urls["number"], urls["table"]);
        }
        
    if (useGroupCode && !codeFound) {
        url = string.Concat(url, "group/");
        }

    <np />
    <np />
    <split>
        <whitespace-preserve>@(string.Format(Resources.FreightDeliveriedFormat, delivery.IsSelfService
            ? Regex.Replace(delivery.DeliveryOperator.GetNameOrEmpty(), "\\d{6}", "")
            : delivery.Courier == null ? string.Empty : @Regex.Replace(delivery.Courier.Name, "\\d{6}", "")) + "\u00a0")</whitespace-preserve>
        <line symbols="_" />
    </split>
    <np />
    <split>
        <whitespace-preserve>@(Resources.FreightReceivedColon + "\u00a0")</whitespace-preserve>
        <line symbols="_" />
    </split>
    <np />
    <left>@Resources.DeliveryProcessingCommentsColon</left>

    if (!string.IsNullOrWhiteSpace(delivery.Comment))
    {
        <left>@delivery.Comment</left>
    }

    <line symbols="_" />
    <line symbols="_" />
    <line symbols="_" />
    <line symbols="_" />
    <np />
    <np />
    <center>@string.Format(Resources.AllSumsInFormat, cafeSetup.CurrencyName)</center>
    <np />
    <whitespace-preserve>@Raw(string.Join(Environment.NewLine, Model.Extensions.BeforeFooter))</whitespace-preserve>
    <center><split><whitespace-preserve>@cafeSetup.BillFooter</whitespace-preserve></split></center>
    <np />
    <np />
    <whitespace-preserve>@Raw(string.Join(Environment.NewLine, Model.Extensions.AfterFooter))</whitespace-preserve>
    <np />

    @* Netmonet (begin) *@
    if (!useGrouping || groupDictionary.ContainsKey(group)) {
        <f2><center>@("Отзывы и чаевые")</center></f2>
        <f2><center>@("нетмонет")</center></f2>
        <np />
        <qrcode size="small" correction="low">@url@code@urlParams@urls["wp"]@wpid</qrcode>
        <np />
        <center>@("Наведите камеру на QR-код")</center>
        <center>@("или введите " + @code + " на netmonet.co")</center>
        }
    @* Netmonet (end) *@
}

@helper Body()
{
    var delivery = Model.Delivery;
    var order = delivery.Order;

    var fullSum = order.GetFullSum();
    var subTotal = fullSum - order.DiscountItems.Where(discountItem => discountItem.IsCategorized && discountItem.PrintDetailedInPrecheque).Sum(discountItem => discountItem.GetDiscountSum());
    var vatSum = order.GetVatSumExcludedFromPrice();
    var resultSum = subTotal - order.DiscountItems.Where(discountItem => !discountItem.IsCategorized || !discountItem.PrintDetailedInPrecheque).Sum(discountItem => discountItem.GetDiscountSum()) + vatSum;
    var changeSum = order.GetChangeSum(resultSum);

    var paymentsAsDiscountSum = order.Payments.Concat(order.PrePayments).Where(pi => pi.Type.ProccessAsDiscount).Sum(t => t.Sum);
    resultSum -= paymentsAsDiscountSum;

    var nonCategorizedDiscounts = (from discountItem in order.DiscountItems
                                   where !discountItem.IsCategorized || !discountItem.PrintDetailedInPrecheque
                                   let discountSum = discountItem.GetDiscountSum()
                                   select new NonCategorizedDiscountItem(
                                       discountItem.Type.PrintableName,
                                       CalculatePercent(fullSum, discountSum),
                                       discountSum,
                                       discountItem.CardInfo != null && !string.IsNullOrWhiteSpace(discountItem.CardInfo.MaskedCard)
                                            ? discountItem.CardInfo.MaskedCard
                                            : string.Empty,
                                       discountItem.Type.DiscountBySum)).ToList();

    foreach (var paymentItem in order.Payments.Concat(order.PrePayments).Where(pi => pi.Type.ProccessAsDiscount && pi.Type is INonCashPaymentType))
    {
        var replaceDiscount = ((INonCashPaymentType)paymentItem.Type).ReplaceDiscount;
        if (replaceDiscount == null)
        {
            continue;
        }
        var iikoNetPaymentItem = paymentItem as IIikoNetPaymentItem;
        if (iikoNetPaymentItem != null)
        {
            var discountSum = iikoNetPaymentItem.DiscountSum;
            var paymentSum = paymentItem.Sum - discountSum;
            //добавляем информацию о платеже. Если скидки нет, информация о платеже добавляется всегда
            if (paymentSum > 0 || discountSum == 0)
            {
                nonCategorizedDiscounts.Add(new NonCategorizedDiscountItem(
                    string.Format(Resources.PaymentWithDiscountSplitFormatPayment, replaceDiscount.PrintableName),
                    CalculatePercent(fullSum, paymentSum),
                    paymentSum,
                    string.Empty,
                    false));
            }
            //добавляем информацию о скидке: не показывается, если скидки нет
            if (discountSum > 0)
            {
                nonCategorizedDiscounts.Add(new NonCategorizedDiscountItem(
                    string.Format(Resources.PaymentWithDiscountSplitFormatDiscount, replaceDiscount.PrintableName),
                    CalculatePercent(fullSum, discountSum),
                    discountSum,
                    string.Empty,
                    false));
            }
        }
        else
        {
            nonCategorizedDiscounts.Add(new NonCategorizedDiscountItem(
                replaceDiscount.PrintableName,
                CalculatePercent(fullSum, paymentItem.Sum),
                paymentItem.Sum,
                string.Empty,
                replaceDiscount.DiscountBySum));
        }
    }

    <left>@string.Format(Resources.GuestsCountFormat, delivery.PersonCount)</left>
    <line />
    @EntriesByGuest()
    <line />
    if (nonCategorizedDiscounts.Count > 0 || vatSum != 0m)
    {
        <pair left="@Resources.BillFooterTotalPlain" right="@FormatMoney(subTotal)" />
    }
    if (nonCategorizedDiscounts.Count > 0)
    {
        <table>
            <columns>
                <column formatter="split" />
                <column align="right" autowidth="" />
                <column align="right" autowidth="" />
            </columns>
            <cells>
                @foreach (var discount in nonCategorizedDiscounts)
                {
                    if (discount.DiscountBySum)
                    {
                        <c colspan="2">@discount.Name</c>
                    }
                    else
                    {
                        <ct>@discount.Name</ct>
                        <ct>@FormatPercent(-discount.Percent)</ct>
                    }

                    <ct>@FormatMoney(-discount.Sum)</ct>
                    if (!string.IsNullOrWhiteSpace(discount.CardNumber))
                    {
                        <c colspan="3">@string.Format(Resources.CardPattern, discount.CardNumber)</c>
                    }
                }
            </cells>
        </table>
    }
    if (vatSum != 0m)
    {
        var vatSumsByVat = order.GetIncludedEntries()
            .Where(orderEntry => !orderEntry.VatIncludedInPrice)
            .GroupBy(orderEntry => orderEntry.Vat)
            .Where(group => group.Key > 0m)
            .Select(group => new { Vat = group.Key, Sum = group.Sum(orderEntry => orderEntry.ExcludedVat) })
            .ToList();

        foreach (var vatWithSum in vatSumsByVat)
        {
            <pair left="@string.Format(Resources.VatFormat, vatWithSum.Vat)" right="@FormatMoney(vatWithSum.Sum)" />
        }
        if (vatSumsByVat.Count > 1)
        {
            <pair left="@Resources.VatSum" right="@FormatMoney(vatSum)" />
        }
    }
    <pair left="@Resources.BillFooterTotalLower" right="@FormatMoney(resultSum)" />

    var payments = order.Payments.Where(payment => !payment.Type.ProccessAsDiscount).ToList();
    var containsAnyPaymentsOrPrePayments = payments.Any() || order.PrePayments.Any();
    var paymentsSumLessThanResultOrderSum = payments.Concat(order.PrePayments).Sum(p => p.Sum) < resultSum;
    if (containsAnyPaymentsOrPrePayments && !paymentsSumLessThanResultOrderSum)
    {
        <line />
        foreach (var paymentItem in order.PrePayments)
        {
            <pair left="@string.Format(Resources.PrepayTemplate, paymentItem.Type.Name)" right="@FormatMoney(paymentItem.Sum)" />
        }

        foreach (var payment in payments.Where(p => p.Type.Group == PaymentGroup.Cash))
        {
            var nameFormat = GetPaymentItemName(payment);
            <pair left="@nameFormat" right="@FormatMoney(payment.Sum)" />
        }

        foreach (var payment in payments.Where(p => p.Type.Group == PaymentGroup.Card))
        {
            var nameFormat = GetPaymentItemName(payment);
            <pair left="@nameFormat" right="@FormatMoney(payment.Sum)" />
        }

        foreach (var payment in payments.Where(p => p.Type.Group != PaymentGroup.Cash && p.Type.Group != PaymentGroup.Card))
        {
            var nameFormat = GetPaymentItemName(payment);
            var iikoNetPaymentItem = payment as IIikoNetPaymentItem;
            if (iikoNetPaymentItem != null)
            {
                var discountSum = iikoNetPaymentItem.DiscountSum;
                var paymentSum = payment.Sum - discountSum;
                //добавляем информацию о платеже. Если скидки нет, информация о платеже добавляется всегда
                if (paymentSum > 0 || discountSum == 0)
                {
                    <pair left="@string.Format(Resources.PaymentWithDiscountSplitFormatPayment, nameFormat)" right="@FormatMoney(paymentSum)" />
                }
                //добавляем информацию о скидке: не показывается, если скидки нет
                if (discountSum > 0)
                {
                    <pair left="@string.Format(Resources.PaymentWithDiscountSplitFormatDiscount, nameFormat)" right="@FormatMoney(discountSum)" />
                }
            }
            else
            {
                <pair left="@nameFormat" right="@FormatMoney(payment.Sum)" />
                var creditPaymentItem = payment as ICreditPaymentItem;
                if (creditPaymentItem != null && creditPaymentItem.Counteragent != null)
                {
                    <left>@string.Format(Resources.CounteragentFormat, creditPaymentItem.Counteragent.Name)</left>
                    <left>@string.Format(Resources.CounteragentCardFormat, creditPaymentItem.Counteragent.Card)</left>
                }
            }
        }

        if (changeSum > Decimal.Zero)
        {
            <pair left="@Resources.Change" right="@FormatMoney(changeSum)" />
        }
    }
}

@helper EntriesByGuest()
{
    <table>
        <columns>
            <column formatter="split" />
            <column align="right" autowidth="" />
            <column align="right" autowidth="" />
            <column align="right" autowidth="" />
        </columns>
        <cells>
            <ct>@Resources.NameColumnHeader</ct>
            <ct>@Resources.AmountShort</ct>
            <ct>@Resources.ProductPrice</ct>
            <ct>@Resources.ResultSum</ct>
            <linecell />

            @if (Model.Delivery.SplitBetweenPersons)
            {
                var firstGuest = Model.Delivery.Order.Guests.First();
                @Guest(firstGuest.Name, firstGuest.Items.ToList())
                foreach (var guest in Model.Delivery.Order.Guests.Skip(1))
                {
                    <linecell symbols=" " />
                    @Guest(guest.Name, guest.Items.ToList())
                }
            }
            else
            {
                @Guest(null, Model.Delivery.Order.Guests.SelectMany(g => g.Items).ToList())
            }
        </cells>
    </table>
}

@helper Guest(string guestName, ICollection<IOrderItem> items)
{
    if (!string.IsNullOrWhiteSpace(guestName))
    {
        <c colspan="4">@guestName</c>
    }
    foreach (var comboGroup in items.Where(item => item.DeletionInfo == null).GroupBy(i => i.Combo))
    {
        var combo = comboGroup.Key;
        if (combo != null)
        {
            <c colspan="4">@combo.Name</c>
            <c colspan="2"><right>@(string.Format("{0} x", FormatAmount(combo.Amount)))</right></c>
            <ct>@FormatMoney(combo.Price)</ct>
            <ct>@FormatMoney(combo.Price * combo.Amount)</ct>
        }
        foreach (var orderItem in comboGroup.OrderBy(i => i.OrderRank))
        {
            @OrderItem(orderItem, combo != null)
        }
    }
    if (!string.IsNullOrWhiteSpace(guestName))
    {
        <c colspan="3" />
        <c><line /></c>
        <c colspan="2">@string.Format(Resources.BillFooterTotalGuestPattern, guestName)</c>
        <c colspan="2"><right>@FormatMoney(items.SelectMany(item => item.ExpandIncludedEntries()).Sum(item => item.Cost))</right></c>
    }
}

@helper OrderItem(IOrderItem item, bool isPartOfCombo)
{
    var productItem = item as IProductItem;
    // для позиций в составе комбо, делается дополнительный отступ
    var additionalSpace = isPartOfCombo ? " " : string.Empty;
    var commentText = Model.Delivery.Order.Table.Section.PrintProductItemCommentInCheque && productItem != null && productItem.Comment != null && !productItem.Comment.Deleted ? productItem.Comment.Text : null;
    if (productItem != null && productItem.CompoundsInfo != null && productItem.CompoundsInfo.IsPrimaryComponent)
    {
        <c colspan="4"><whitespace-preserve>@(additionalSpace + string.Format("{0} {1}", productItem.CompoundsInfo.ModifierSchemaName, productItem.ProductSize == null ? string.Empty : productItem.ProductSize.Name))</whitespace-preserve></c>

        // для разделенной пиццы комменты печатаем под схемой
        if (commentText != null)
        {
            <c colspan="4"><whitespace-preserve>@(additionalSpace + "  " + commentText)</whitespace-preserve></c>
        }

        // у пиццы не может быть удаленных модификаторов, поэтому берем весь список
        foreach (var orderEntry in productItem.ModifierEntries.Where(orderEntry => ModifiersFilter(orderEntry, productItem, true)))
        {
            var productName = orderEntry.ProductCustomName ?? orderEntry.Product.Name;
            <c colspan="4"><whitespace-preserve>@(additionalSpace + "  " + productName)</whitespace-preserve></c>

            if (orderEntry.Cost != 0m)
            {
                <c colspan="2"><whitespace-preserve><right>@(additionalSpace + string.Format("{0} {1} x", FormatAmount(orderEntry.Amount), orderEntry.Product.MeasuringUnit.Name))</right></whitespace-preserve></c>
                <ct>@FormatMoney(orderEntry.Price)</ct>
                <ct>@FormatMoney(orderEntry.Cost)</ct>
            }
            else
            {
                <c colspan="2"><whitespace-preserve><right>@(additionalSpace + string.Format("{0} {1}  ", FormatAmount(orderEntry.Amount), orderEntry.Product.MeasuringUnit.Name))</right></whitespace-preserve></c>
                <c colspan="2" />
            }

            @PrintOrderEntryAllergens(orderEntry)
            @CategorizedDiscountsForOrderEntry(orderEntry, isPartOfCombo)
        }
    }

    if (productItem != null && productItem.CompoundsInfo != null)
    {
        <c colspan="4"><whitespace-preserve>@(additionalSpace + " 1/2 " + GetOrderEntryNameWithProductSize(productItem))</whitespace-preserve></c>
    }
    else
    {
        <c colspan="4"><whitespace-preserve>@(additionalSpace + GetOrderEntryNameWithProductSize(item))</whitespace-preserve></c>
        @* для обычных блюд перед списком модификаторов *@
        if (commentText != null)
        {
            <c colspan="4"><whitespace-preserve>@(additionalSpace + "  " + commentText)</whitespace-preserve></c>
        }
    }

    if (!isPartOfCombo)
    {
        <c colspan="2"><right>@string.Format("{0} {1} x", FormatAmount(item.Amount), item.Product.MeasuringUnit.Name)</right></c>
        <ct>@FormatMoney(item.Price)</ct>
        <ct>@FormatMoney(item.Cost)</ct>
    }
    else
    {
        <c colspan="2"><right><whitespace-preserve>@string.Format("{0} {1}  ", FormatAmount(item.Amount), item.Product.MeasuringUnit.Name)</whitespace-preserve></right></c>
        <c colspan="2" />
    }

    if (productItem != null)
    {
        @PrintOrderEntryAllergens(productItem)
    }

    @CategorizedDiscountsForOrderEntry(item, isPartOfCombo)

    foreach (var orderEntry in item.GetNotDeletedChildren().Where(orderEntry => ModifiersFilter(orderEntry, item))
        .Where(orderEntry => orderEntry.Cost > 0 || orderEntry.Product.PrechequePrintable).ToList())
    {
        var productName = orderEntry.ProductCustomName ?? orderEntry.Product.Name;
        <c colspan="4"><whitespace-preserve>@(additionalSpace + "  " + productName)</whitespace-preserve></c>

        if (orderEntry.Cost != 0m)
        {
            <c colspan="2"><right>@string.Format("{0} {1} x", FormatAmount(orderEntry.Amount), orderEntry.Product.MeasuringUnit.Name)</right></c>
            <ct>@FormatMoney(orderEntry.Price)</ct>
            <ct>@FormatMoney(orderEntry.Cost)</ct>
        }
        else
        {
            <c colspan="2"><right><whitespace-preserve>@string.Format("{0} {1}  ", FormatAmount(orderEntry.Amount), orderEntry.Product.MeasuringUnit.Name)</whitespace-preserve></right></c>
            <c colspan="2" />
        }

        @PrintOrderEntryAllergens(orderEntry)
        @CategorizedDiscountsForOrderEntry(orderEntry, isPartOfCombo)
    }
}

@helper CategorizedDiscountsForOrderEntry(IOrderEntry entry, bool isPartOfCombo)
{
    var additionalSpace = isPartOfCombo ? " " : string.Empty;
    var categorizedDiscounts = from discountItem in Model.Delivery.Order.DiscountItems.ToList().Where(discountItem => discountItem.IsCategorized && discountItem.PrintDetailedInPrecheque).ToList()
                               where discountItem.DiscountSums.ContainsKey(entry)
                               let discountSum = discountItem.GetDiscountSumFor(entry)
                               select new
                               {
                                   Name = discountItem.Type.PrintableName,
                                   Percent = Math.Round(CalculatePercent(entry.Cost, discountSum)),
                                   Sum = discountSum,
                                   CardNumber = discountItem.CardInfo != null && !string.IsNullOrWhiteSpace(discountItem.CardInfo.MaskedCard)
                                       ? discountItem.CardInfo.MaskedCard
                                       : string.Empty,
                                   discountItem.Type.DiscountBySum
                               };

    foreach (var discount in categorizedDiscounts)
    {
        if (discount.DiscountBySum)
        {
            <c colspan="3"><whitespace-preserve>@(additionalSpace + string.Format("   {0}", discount.Name))</whitespace-preserve></c>
        }
        else
        {
            <c colspan="3"><whitespace-preserve>@(additionalSpace + string.Format("   {0} ({1})", discount.Name, FormatPercent(-discount.Percent)))</whitespace-preserve></c>
        }

        <ct>@FormatMoney(-discount.Sum)</ct>
        if (!string.IsNullOrWhiteSpace(discount.CardNumber))
        {
            <c colspan="4">@string.Format(Resources.CardPattern, discount.CardNumber)</c>
        }
    }
}

@helper PrintOrderEntryAllergens(IOrderEntry orderEntry)
{
    var totalAllergens = orderEntry.Allergens.ToArray();
    if (totalAllergens.Any())
    {
        <c colspan="0">@( string.Format(Resources.AllergenGroupsFormat, string.Join(", ", totalAllergens)) )</c>
    }
}

@functions
{
    private static bool CommonModifiersFilter(bool isCommonModifier, IProductItem parent, bool onlyCommonModifiers)
    {
        if (parent.CompoundsInfo != null && parent.CompoundsInfo.IsPrimaryComponent)
        {
            if (onlyCommonModifiers && !isCommonModifier)
                return false;
            if (!onlyCommonModifiers && isCommonModifier)
                return false;
            return true;
        }
        return true;
    }

    private static bool ModifiersFilter(IOrderEntry orderEntry, IOrderItem parent, bool onlyCommonModifiers = false)
    {
        var parentProductItem = parent as IProductItem;
        if (parentProductItem != null && !CommonModifiersFilter(((IModifierEntry)orderEntry).IsCommonModifier, parentProductItem, onlyCommonModifiers))
            return false;

        if (orderEntry.Cost > 0m)
            return true;

        if (!orderEntry.Product.PrechequePrintable)
            return false;

        var modifierEntry = orderEntry as IModifierEntry;
        if (modifierEntry == null)
            return true;

        if (modifierEntry.ChildModifier == null)
            return true;

        if (!modifierEntry.ChildModifier.HideIfDefaultAmount)
            return true;

        var amountPerItem = modifierEntry.ChildModifier.AmountIndependentOfParentAmount
            ? modifierEntry.Amount
            : modifierEntry.Amount / GetParentAmount(parent, onlyCommonModifiers);

        return amountPerItem != modifierEntry.ChildModifier.DefaultAmount;
    }

    private static decimal GetParentAmount(IOrderItem parent, bool onlyCommonModifiers)
    {
        return onlyCommonModifiers ? parent.Amount * 2 : parent.Amount;
    }

    private static string GetPaymentItemName(IPaymentItem payment)
    {
        var nameFormat = payment.Type.Group == PaymentGroup.Card
            ? string.Format(Resources.CardPattern, payment.Type.Name)
            : payment.Type.Name;

        return payment.IsProcessedExternally
            ? string.Format(Resources.PaymentItemIsProcessedNameFormat, nameFormat)
            : nameFormat;
    }

    private sealed class NonCategorizedDiscountItem
    {
        public NonCategorizedDiscountItem(string name, decimal percent, decimal sum, string cardNumber, bool discountBySum)
        {
            Name = name;
            Percent = percent;
            Sum = sum;
            CardNumber = cardNumber;
            DiscountBySum = discountBySum;
        }

        public string Name { get; private set; }
        public decimal Percent { get; private set; }
        public decimal Sum { get; private set; }
        public string CardNumber { get; private set; }
        public bool DiscountBySum { get; private set; }
    }

    private static string GetOrderEntryNameWithProductSize(IOrderEntry orderEntry)
    {
        var productName = orderEntry.ProductCustomName ?? orderEntry.Product.Name;
        var productItem = orderEntry as IProductItem;
        return productItem == null || productItem.ProductSize == null || productItem.CompoundsInfo != null
            ? productName
            : string.Format(Resources.ProductNameWithSizeFormat, productName, productItem.ProductSize.Name);
    }
}
